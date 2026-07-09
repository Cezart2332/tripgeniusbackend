using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TripGeniusBackend.Application.Agent;
using TripGeniusBackend.Application.DTOs.Trip;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Interfaces.UseCases;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Domain.Enums;
using TripGeniusBackend.Infrastructure.Persistence.Hubs;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

/// <summary>
/// Runs the in-trip AI agent when a user writes "@ai" in a trip/offroad group chat.
/// Streams the reply to the trip group and persists it as an AI-authored message.
/// </summary>
public class TripChatAiService : ITripChatAiService
{
    private readonly IAgentRunner _agentRunner;
    private readonly ITripService _tripService;
    private readonly ITripRepository _tripRepository;
    private readonly IOffroadTripRepository _offroadTripRepository;
    private readonly IMessageRepository _messageRepository;
    private readonly IOffroadMessageRepository _offroadMessageRepository;
    private readonly IMessageQueryService _messageQueryService;
    private readonly IOffroadMessageQueryService _offroadMessageQueryService;
    private readonly IHubContext<TripChatHub> _tripHub;
    private readonly IHubContext<OffroadTripChatHub> _offroadHub;
    private readonly ILogger<TripChatAiService> _logger;

    private const int HistoryTurns = 12;

    public TripChatAiService(
        IAgentRunner agentRunner,
        ITripService tripService,
        ITripRepository tripRepository,
        IOffroadTripRepository offroadTripRepository,
        IMessageRepository messageRepository,
        IOffroadMessageRepository offroadMessageRepository,
        IMessageQueryService messageQueryService,
        IOffroadMessageQueryService offroadMessageQueryService,
        IHubContext<TripChatHub> tripHub,
        IHubContext<OffroadTripChatHub> offroadHub,
        ILogger<TripChatAiService> logger)
    {
        _agentRunner = agentRunner;
        _tripService = tripService;
        _tripRepository = tripRepository;
        _offroadTripRepository = offroadTripRepository;
        _messageRepository = messageRepository;
        _offroadMessageRepository = offroadMessageRepository;
        _messageQueryService = messageQueryService;
        _offroadMessageQueryService = offroadMessageQueryService;
        _tripHub = tripHub;
        _offroadHub = offroadHub;
        _logger = logger;
    }

    public async Task RespondInTripAsync(int tripId, int userId, string userMessage, CancellationToken ct = default)
    {
        var trip = await _tripRepository.GetTripById(tripId);
        if (trip == null) return;

        var member = trip.Members.FirstOrDefault(m => m.UserId == userId && m.MemberStatus == MemberStatus.Accepted);
        if (member == null) return; // not a member — ignore
        bool canEdit = member.Role is Roles.Owner or Roles.Admin;

        var context = BuildTripContext(trip);
        var tools = canEdit ? BuildTripTools(tripId, userId) : new List<AgentTool>();
        var systemPrompt = BuildSystemPrompt("trip", trip.Title, context, canEdit);
        var history = ToHistory(await _messageQueryService.GetMessages(tripId));

        var groupName = $"trip-{tripId}";
        await RunAndBroadcastAsync(
            _tripHub, groupName, systemPrompt, userMessage, history, tools,
            persist: content => PersistTripAiMessageAsync(tripId, content),
            ct);
    }

    public async Task RespondInOffroadAsync(int offroadTripId, int userId, string userMessage, CancellationToken ct = default)
    {
        var trip = await _offroadTripRepository.GetTripById(offroadTripId);
        if (trip == null) return;

        var member = trip.Members.FirstOrDefault(m => m.UserId == userId && m.MemberStatus == MemberStatus.Accepted);
        if (member == null) return;

        var context = BuildOffroadContext(trip);
        // Offroad route mutation needs geometry resolution — v1 agent is read-only Q&A here.
        var systemPrompt = BuildSystemPrompt("offroad", trip.Title, context, canEdit: false);
        var history = ToHistory(await _offroadMessageQueryService.GetMessages(offroadTripId));

        var groupName = $"offroad-{offroadTripId}";
        await RunAndBroadcastAsync(
            _offroadHub, groupName, systemPrompt, userMessage, history, new List<AgentTool>(),
            persist: content => PersistOffroadAiMessageAsync(offroadTripId, content),
            ct);
    }

    /// <summary>Maps recent chat messages to agent turns (AI → assistant; others → "user: name says ...").</summary>
    private static List<AgentMessage> ToHistory(List<MessageResponse> messages)
    {
        // Drop the just-sent "@ai ..." message (it is passed separately as the current turn).
        var recent = messages.TakeLast(HistoryTurns + 1).ToList();
        if (recent.Count > 0) recent.RemoveAt(recent.Count - 1);

        return recent.Select(m => new AgentMessage
        {
            Role = m.IsAi ? "assistant" : "user",
            Content = m.IsAi ? m.Content : $"{m.Username}: {m.Content}"
        }).ToList();
    }

    // ─────────────────────────────────────────────────────────────────────

    private async Task RunAndBroadcastAsync<THub>(
        IHubContext<THub> hub, string groupName, string systemPrompt, string userMessage,
        List<AgentMessage> history, List<AgentTool> tools, Func<string, Task<MessageResponse>> persist, CancellationToken ct)
        where THub : Hub
    {
        await hub.Clients.Group(groupName).SendAsync("AiMessageStart", cancellationToken: ct);

        AgentRunResult result;
        try
        {
            result = await _agentRunner.RunAsync(
                new AgentRequest
                {
                    SystemPrompt = systemPrompt,
                    UserMessage = userMessage,
                    History = history,
                    Tools = tools,
                    EnableWebSearch = true
                },
                onChunk: chunk => hub.Clients.Group(groupName).SendAsync("AiMessageChunk", chunk, ct),
                ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trip agent failed for group {Group}", groupName);
            result = new AgentRunResult { FinalText = "Sorry, I couldn't complete that right now. Please try again." };
        }

        var text = string.IsNullOrWhiteSpace(result.FinalText)
            ? "Done."
            : result.FinalText;

        var saved = await persist(text);
        await hub.Clients.Group(groupName).SendAsync("AiMessageEnd", saved, ct);

        if (result.AnyToolSucceeded)
            await hub.Clients.Group(groupName).SendAsync("TripUpdated", cancellationToken: ct);
    }

    private async Task<MessageResponse> PersistTripAiMessageAsync(int tripId, string content)
    {
        var message = Message.CreateAi(content, DateTime.UtcNow, tripId);
        await _messageRepository.AddMessage(message);
        await _messageRepository.SaveChanges();
        return ToAiResponse(message.Id, content, message.Date);
    }

    private async Task<MessageResponse> PersistOffroadAiMessageAsync(int offroadTripId, string content)
    {
        var message = OffroadMessage.CreateAi(content, DateTime.UtcNow, offroadTripId);
        await _offroadMessageRepository.AddMessage(message);
        await _offroadMessageRepository.SaveChanges();
        return ToAiResponse(message.Id, content, message.Date);
    }

    private static MessageResponse ToAiResponse(int id, string content, DateTime date) => new()
    {
        Id = id,
        Content = content,
        SentAt = date,
        ImageUrl = "",
        Username = "TripGenius AI",
        ProfileUrl = "",
        IsAi = true
    };

    // ─────────────────────────────────────────────────────────────────────
    //  Prompt & context
    // ─────────────────────────────────────────────────────────────────────

    private static string BuildSystemPrompt(string kind, string title, string context, bool canEdit)
    {
        var today = DateTime.UtcNow.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);
        var editLine = canEdit
            ? """
              You MAY modify this trip using the provided tools (add/update/remove activities, add a day).
              Fill the itinerary row PROPERLY — do not add a bare item. Every activity should have:
              a specific named place, the correct type (Accommodation/Food/Attraction/...), its price
              (cost in RON) when it has one, and a link to the place. Use web_search to find a real place,
              its typical price, and a booking or official link before adding.
              If the request is too vague to fill the row well (missing which place, which day, or the
              budget matters and you don't know it), ask ONE short clarifying question in chat and do NOT
              call a tool yet — wait for the reply. Only add once you have enough to fill the row nicely.
              After acting, briefly confirm what you added (place, day, price).
              """
            : "You can only ANSWER questions about this trip — you must NOT modify it (the user lacks permission).";

        return $"""
        You are TripGenius AI, a helpful assistant inside the group chat of a specific {kind}.
        Today is {today}. Respond in the user's language, in 1–3 short sentences.

        {editLine}

        Stay strictly on THIS {kind}; do not discuss other trips. For real-world facts (weather,
        opening hours, prices, current status of a place) use web_search and never guess from memory.

        CURRENT {kind.ToUpperInvariant()} — "{title}":
        {context}
        """;
    }

    private static string BuildTripContext(Trip trip)
    {
        var sb = new StringBuilder();
        foreach (var tl in trip.Timelines.OrderBy(t => t.StartDay))
        {
            var days = tl.StartDay == tl.EndDay ? $"Day {tl.StartDay}" : $"Day {tl.StartDay}-{tl.EndDay}";
            sb.AppendLine($"{days}: {tl.StartingPoint} → {tl.EndPoint}. Note: {tl.Note}");
            foreach (var a in tl.Activities)
                sb.AppendLine($"   - [activityId={a.Id}] {a.Name} ({a.Type}, {a.Cost} RON){(string.IsNullOrWhiteSpace(a.Link) ? "" : $" {a.Link}")}");
        }
        return sb.Length == 0 ? "(no days planned yet)" : sb.ToString().TrimEnd();
    }

    private static string BuildOffroadContext(OffroadTrip trip)
    {
        var sb = new StringBuilder();
        foreach (var r in trip.Routes.OrderBy(r => r.StartDay))
        {
            var days = r.StartDay == r.EndDay ? $"Day {r.StartDay}" : $"Day {r.StartDay}-{r.EndDay}";
            sb.AppendLine($"{days}: {r.Name} — {r.DistanceMeters / 1000.0:F1} km, +{r.ElevationGainMeters:F0} m. Note: {r.Note}");
        }
        return sb.Length == 0 ? "(no routes yet)" : sb.ToString().TrimEnd();
    }

    // ─────────────────────────────────────────────────────────────────────
    //  Trip agent tools (Owner/Admin only)
    // ─────────────────────────────────────────────────────────────────────

    private List<AgentTool> BuildTripTools(int tripId, int userId)
    {
        var activityTypes = Enum.GetNames<ActivityType>();

        return new List<AgentTool>
        {
            new()
            {
                Name = "add_activity",
                Description = "Add a new activity to a specific day of this trip.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        day = new { type = "integer", description = "The day number in the itinerary." },
                        name = new { type = "string" },
                        description = new { type = "string" },
                        type = new { type = "string", @enum = activityTypes },
                        cost = new { type = "number", description = "Estimated cost in RON. Use 0 if unknown." },
                        link = new { type = "string", description = "A URL for the place, if known." }
                    },
                    required = new[] { "day", "name", "type" }
                },
                Handler = args => _tripService.AgentAddActivity(userId, tripId,
                    GetInt(args, "day"),
                    new TripActivityRequest
                    {
                        Name = GetStr(args, "name"),
                        Description = GetStr(args, "description"),
                        Type = ParseType(GetStr(args, "type")),
                        Cost = GetDouble(args, "cost"),
                        Link = GetStr(args, "link")
                    })
            },
            new()
            {
                Name = "add_day",
                Description = "Add a new day (timeline segment) to this trip.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        startDay = new { type = "integer" },
                        endDay = new { type = "integer" },
                        startingPoint = new { type = "string" },
                        endPoint = new { type = "string" },
                        note = new { type = "string" }
                    },
                    required = new[] { "startDay", "endDay", "startingPoint", "endPoint" }
                },
                Handler = args => _tripService.AgentAddDay(userId, tripId,
                    GetInt(args, "startDay"), GetInt(args, "endDay"),
                    GetStr(args, "startingPoint"), GetStr(args, "endPoint"), GetStr(args, "note"))
            },
            new()
            {
                Name = "update_activity",
                Description = "Update an existing activity. Use the activityId shown in the itinerary context.",
                Parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        activityId = new { type = "integer" },
                        name = new { type = "string" },
                        description = new { type = "string" },
                        type = new { type = "string", @enum = activityTypes },
                        cost = new { type = "number" },
                        link = new { type = "string" }
                    },
                    required = new[] { "activityId", "name", "type" }
                },
                Handler = args => _tripService.AgentUpdateActivity(userId, tripId,
                    GetInt(args, "activityId"),
                    new TripActivityRequest
                    {
                        Name = GetStr(args, "name"),
                        Description = GetStr(args, "description"),
                        Type = ParseType(GetStr(args, "type")),
                        Cost = GetDouble(args, "cost"),
                        Link = GetStr(args, "link")
                    })
            },
            new()
            {
                Name = "remove_activity",
                Description = "Remove an activity by its activityId (shown in the itinerary context).",
                Parameters = new
                {
                    type = "object",
                    properties = new { activityId = new { type = "integer" } },
                    required = new[] { "activityId" }
                },
                Handler = args => _tripService.AgentRemoveActivity(userId, tripId, GetInt(args, "activityId"))
            }
        };
    }

    // ── JSON argument helpers ──
    private static int GetInt(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v)
            ? (v.ValueKind == JsonValueKind.Number ? v.GetInt32()
               : int.TryParse(v.GetString(), out var i) ? i : 0)
            : 0;

    private static string GetStr(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";

    private static double GetDouble(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;

    private static ActivityType ParseType(string s) =>
        Enum.TryParse<ActivityType>(s, ignoreCase: true, out var t) ? t : ActivityType.Other;
}
