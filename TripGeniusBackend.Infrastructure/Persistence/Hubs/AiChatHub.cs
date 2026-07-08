using System.Security.Claims;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pgvector;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Infrastructure.Persistence.Services;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

[Authorize]
public class AiChatHub : Hub
{
    private readonly IAiChatRepository _aiChatRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAiMemoryRepository _aiMemoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IAiChatQueryService _aiChatQueryService;
    private readonly IAiService _aiService;
    private readonly ILinkValidationService _linkValidationService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITripRepository _tripRepository;
    private readonly IOffroadTripRepository _offroadTripRepository;
    private readonly ILogger<AiChatHub> _logger;

    public AiChatHub(
        IAiChatRepository aiChatRepository,
        IUserRepository userRepository,
        IAiChatQueryService aiChatQueryService,
        IAiService aiService,
        IAiMemoryRepository aiMemoryRepository,
        IEmbeddingService embeddingService,
        IServiceScopeFactory scopeFactory,
        ITripRepository tripRepository,
        IOffroadTripRepository offroadTripRepository,
        ILinkValidationService linkValidationService,
        ILogger<AiChatHub> logger)
    {
        _aiChatRepository = aiChatRepository;
        _userRepository = userRepository;
        _aiChatQueryService = aiChatQueryService;
        _aiService = aiService;
        _aiMemoryRepository = aiMemoryRepository;
        _embeddingService = embeddingService;
        _scopeFactory = scopeFactory;
        _tripRepository = tripRepository;
        _offroadTripRepository = offroadTripRepository;
        _linkValidationService = linkValidationService;
        _logger = logger;
    }

    private int GetUserId()
    {
        var value = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(value, out var userId))
            throw new HubException("Unauthorized");
        return userId;
    }

    public async Task JoinAiChat()
    {
        var userId = GetUserId();
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());

    }
    public async Task LeaveAiChat()
    {
        var userId = GetUserId();
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
    }

    private static int CountNumberedRecommendations(string text) =>
        Regex.Matches(text, @"(?m)^\s*\d+\.\s+").Count;

    /// <summary>
    /// Keeps intro text and only the first N numbered list items so body matches verified link cards.
    /// </summary>
    private static string TrimExcessNumberedRecommendations(string text, int keepCount)
    {
        if (keepCount <= 0) return text;

        var lines = text.Split('\n');
        var result = new List<string>();
        var numberedKept = 0;

        foreach (var line in lines)
        {
            if (Regex.IsMatch(line, @"^\s*\d+\.\s+"))
            {
                numberedKept++;
                if (numberedKept > keepCount)
                    continue;
            }
            result.Add(line);
        }

        return string.Join('\n', result).TrimEnd();
    }

    /// <summary>
    /// Parses [LINKS:...] and applies lightweight sanitization (model verifies via web_fetch).
    /// </summary>
    private async Task<(string sanitizedText, List<LinkCard> links)> PostProcessLinksAsync(string rawResponse)
    {
        var match = Regex.Match(rawResponse, @"\[LINKS:(.*?)\]+\s*$", RegexOptions.Singleline);
        if (!match.Success)
            return (rawResponse.Trim(), []);

        try
        {
            var jsonStr = match.Groups[1].Value.Trim();
            using var doc = JsonDocument.Parse(jsonStr);
            var rawLinks = new List<LinkCard>();

            // AI may return "title"/"url" or "Title"/"Url" — handle both
            if (doc.RootElement.TryGetProperty("links", out var linksElement))
            {
                foreach (var item in linksElement.EnumerateArray())
                {
                    var title = (item.TryGetProperty("title", out var t) ? t.GetString()
                               : item.TryGetProperty("Title", out var t2) ? t2.GetString() : "") ?? "";
                    var url = (item.TryGetProperty("url", out var u) ? u.GetString()
                             : item.TryGetProperty("Url", out var u2) ? u2.GetString() : "") ?? "";
                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(url))
                    {
                        rawLinks.Add(new LinkCard { Title = title, Url = url });
                    }
                }
            }

            var validatedLinks = await _linkValidationService.ValidateAndRepairLinksAsync(rawLinks);
            var sanitizedText = Regex.Replace(rawResponse, @"\[LINKS:.*$", "", RegexOptions.Singleline).Trim();
            var numberedInText = CountNumberedRecommendations(sanitizedText);

            if (numberedInText > validatedLinks.Count && validatedLinks.Count > 0)
                sanitizedText = TrimExcessNumberedRecommendations(sanitizedText, validatedLinks.Count);

            return (sanitizedText, validatedLinks);
        }
        catch (Exception ex)
        {
            // On parse error, strip the malformed block and return empty links
            _logger.LogWarning(ex, "Failed to parse LINKS block");
            var fallbackText = Regex.Replace(rawResponse, @"\[LINKS:.*$", "", RegexOptions.Singleline).Trim();
            return (fallbackText, []);
        }
    }

    public async Task SendAiMessage(string content, bool preferProfile)
    {
        var userId = GetUserId();
        var user = await _userRepository.GetUserById(userId);
        if (user == null) throw new KeyNotFoundException("User not found");
        var preferences = user.Preferences;
        // Load history before persisting this turn so AskAsync does not see the same user text twice.
        var lastMessages = await _aiChatQueryService.GetShortTermMemory(userId);

        var aiRequest = new AiChatHistory
        {
            Content = content,
            UserId = userId,
            Role = "user",
            SentAt = DateTime.UtcNow
        };
        await _aiChatRepository.Create(aiRequest);
        await _aiChatRepository.SaveChanges();
        await Clients.Group(userId.ToString()).SendAsync("StartAiMessage");
        await Clients.Group(userId.ToString()).SendAsync("StatusUpdate", AiActivityStatus.Analyzing);
        string fullResponse = "";

        // Parallelize embeddings (HttpClient) but run DB queries sequentially (DbContext is not thread-safe)
        var queryEmbedTask = _embeddingService.GetEmbedding(content);
        var tripEmbedTask = _embeddingService.GetEmbedding($"{content} {string.Join(", ", preferences.Tags)} {preferences.MaxGroupSize}");
        await Task.WhenAll(queryEmbedTask, tripEmbedTask);
        var querryEmbedding = await queryEmbedTask;
        var tripEmbedding = await tripEmbedTask;

        // Run vector searches sequentially to avoid DbContext concurrency issues
        var memories = await _aiMemoryRepository.SearchSimilarAsync(querryEmbedding, userId);
        var trips = await _tripRepository.SearchSimilarAsync(tripEmbedding, userId);
        var offroadTrips = await _offroadTripRepository.SearchSimilarAsync(tripEmbedding, userId);
        var memoryContext = memories.Any()
            ? "WHAT YOU KNOW ABOUT THIS USER (use for vague food/activity asks; ignore when the "
              + "current message states specific style, cuisine, budget, or occasion):\n"
              + string.Join("\n", memories.Select(m => $"- {m.Content}"))
            : "";

        var tripsContext = trips.Any()
            ? "\nRELEVANT TRIPS FROM THE APP:\n" + string.Join("\n", trips.Select(t =>
            {
                var timelinesText = string.Join("\n  ", t.Timelines.Select(tl =>
                {
                    var activitiesText = tl.Activities.Any()
                        ? string.Join(", ", tl.Activities.Select(a => $"{a.Name}({a.Type}, {a.Cost} RON)"))
                        : "no activities";

                    return $"Day {tl.StartDay}-{tl.EndDay}: {tl.StartingPoint} → {tl.EndPoint} | Note: {tl.Note} | Activities: {activitiesText}";
                }));

                return $"- Id:{t.Id} | {t.Title} | {t.Description} | Tags: {string.Join(",", t.Tags)} | Price: {t.Price}\n  {timelinesText}";
            }))
            : "";

        var offroadTripsContext = offroadTrips.Any()
            ? "\nRELEVANT OFFROAD TRIPS FROM THE APP:\n" + string.Join("\n", offroadTrips.Select(t =>
            {
                var routesText = string.Join("\n  ", t.Routes.Select(r =>
                    $"Day {r.StartDay}-{r.EndDay}: {r.Name} | {r.DistanceMeters}m | {r.ElevationGainMeters}m elev | {r.Source} | Note: {r.Note}"));
                return $"- Id:{t.Id} | {t.Title} | {t.Description} | Tags: {string.Join(",", t.Tags)} | Price: {t.Price}\n  {routesText}";
            }))
            : "";

        tripsContext += offroadTripsContext;

        var preferencesContext = "";
        if (preferProfile)
        {
            preferencesContext = preferences != null
                ? $"\nUSER PREFERENCES:\n- Tags: {string.Join(", ", preferences.Tags)}\n- Max group size: {preferences.MaxGroupSize}"
                : "";
        }

        var inLinksBlock = false;
        await _aiService.AskAsync(lastMessages, content, memoryContext, tripsContext, preferencesContext, async (chunk) =>
        {
            fullResponse += chunk;
            if (inLinksBlock)
                return;

            var visibleChunk = chunk;
            var linksStart = chunk.IndexOf("[LINKS:", StringComparison.Ordinal);
            if (linksStart >= 0)
            {
                inLinksBlock = true;
                visibleChunk = chunk[..linksStart];
            }

            var clientChunk = AiResponseSanitizer.StripInternalPrompts(visibleChunk);
            if (!string.IsNullOrEmpty(clientChunk))
                await Clients.Group(userId.ToString()).SendAsync("ReceiveAiChunk", clientChunk);
        },
        async (status) =>
        {
            await Clients.Group(userId.ToString()).SendAsync("StatusUpdate", status);
        });

        fullResponse = AiResponseSanitizer.StripInternalPrompts(fullResponse);
        var (sanitizedText, validatedLinks) = await PostProcessLinksAsync(fullResponse);
        var sanitizedResponse = sanitizedText;
        if (validatedLinks.Count > 0)
        {
            var camelCaseOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var linksJson = JsonSerializer.Serialize(new { links = validatedLinks }, camelCaseOptions);
            sanitizedResponse += $"\n\n[LINKS:{linksJson}]";
        }

        // Emit finalized message (client will replace the streamed version)
        await Clients.Group(userId.ToString()).SendAsync("FinalizeAiMessage", sanitizedResponse);
        await Clients.Group(userId.ToString()).SendAsync("EndAiMessage");

        // Save sanitized response to database
        var aiResponse = new AiChatHistory
        {
            Content = sanitizedResponse,
            UserId = userId,
            Role = "assistant",
            SentAt = DateTime.UtcNow
        };
        await _aiChatRepository.Create(aiResponse);
        await _aiChatRepository.SaveChanges();

        _ = Task.Run(async () =>
        {
            using var scope = _scopeFactory.CreateScope();
            var _memoryRepository = scope.ServiceProvider.GetRequiredService<IAiMemoryRepository>();
            var _embeddingService = scope.ServiceProvider.GetRequiredService<IEmbeddingService>();
            var _aiService = scope.ServiceProvider.GetRequiredService<IAiService>();

            await ExtractAndSaveMemoryAsync(
                content, fullResponse, userId,
                _memoryRepository, _embeddingService, _aiService
            );
        });
    }
    private async Task ExtractAndSaveMemoryAsync(
        string userMessage, string aiResponse, int userId,
        IAiMemoryRepository _memoryRepository,
        IEmbeddingService _embeddingService,
        IAiService _aiService)
    {
        
            string extractionPrompt = $$"""
                You are a memory extraction assistant. Analyze the conversation below and extract ONLY information worth remembering about the user's travel preferences, plans, or personal facts.

                RULES:
                - Extract preferences (e.g. "prefers mountains over beach", "likes budget travel")
                - Extract travel plans (e.g. "wants to visit Japan next summer")
                - Extract personal facts relevant to travel (e.g. "travels with family", "afraid of flying")
                - Store the memory in the SAME LANGUAGE the user used in the conversation
                - If nothing is worth remembering, return an empty array
                - Be concise — each memory should be one short sentence
                - Do NOT extract generic information or AI responses

                Conversation:
                User: {{userMessage}}
                Assistant: {{aiResponse}}

                Respond ONLY with valid JSON, no extra text, no markdown:
                {"memories": ["memory1", "memory2"]}
                """;
            string extractedJson = "";
            extractedJson = await _aiService.ExtractAsync(extractionPrompt);

            extractedJson = extractedJson
                .Replace("```json", "")
                .Replace("```", "")
                .Trim();

            List<string> memoryTexts;
            using (var doc = JsonDocument.Parse(extractedJson))
            {
                memoryTexts = doc.RootElement
                    .GetProperty("memories")
                    .EnumerateArray()
                    .Select(m => m.GetString() ?? "")
                    .Where(m => !string.IsNullOrEmpty(m))
                    .ToList();
            }


            foreach (var text in memoryTexts)
            {
                try
                {
                    var embedding = await _embeddingService.GetEmbedding(text);
                    _memoryRepository.Create(new AiMemory
                    {
                        UserId = userId,
                        Content = text,
                        Embedding = new Vector(embedding),
                        MemoryType = "Preference"
                    });
                    await _memoryRepository.SaveChanges();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to save AI memory");
                }
            }
    }
}