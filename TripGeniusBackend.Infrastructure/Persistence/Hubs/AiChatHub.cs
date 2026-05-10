using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Pgvector;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Queries;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;
using TripGeniusBackend.Infrastructure.Persistence.Repositories;

namespace TripGeniusBackend.Infrastructure.Persistence.Hubs;

public class AiChatHub : Hub
{
    private readonly IAiChatRepository _aiChatRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAiMemoryRepository _aiMemoryRepository;
    private readonly IEmbeddingService _embeddingService;
    private readonly IAiChatQueryService _aiChatQueryService;
    private readonly IAiService _aiService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ITripRepository _tripRepository;
    public AiChatHub(IAiChatRepository aiChatRepository,IUserRepository userRepository,IAiChatQueryService aiChatQueryService, IAiService aiService, IAiMemoryRepository aiMemoryRepository, IEmbeddingService embeddingService,IServiceScopeFactory scopeFactory, ITripRepository tripRepository)
    {
        _aiChatRepository = aiChatRepository;
        _userRepository = userRepository;
        _aiChatQueryService = aiChatQueryService;
        _aiService = aiService;
        _aiMemoryRepository = aiMemoryRepository;
        _embeddingService = embeddingService;
        _scopeFactory = scopeFactory;
        _tripRepository = tripRepository;
    }

    public async Task JoinAiChat()
    {
        var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await Groups.AddToGroupAsync(Context.ConnectionId, userId.ToString());
        
    }
    public async Task LeaveAiChat()
    {
        var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId.ToString());
    }

    public async Task SendAiMessage(string content, bool preferProfile)
    {
        var userId = int.Parse(Context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        var user = await _userRepository.GetUserById(userId);
        var preferences = user?.Preferences;
        if (user == null) throw new KeyNotFoundException("User not found");
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
        string fullResponse = "";
        var lastMessages = await _aiChatQueryService.GetShortTermMemory(userId);
        var querryEmbedding = await _embeddingService.GetEmbedding(content);
        var tripEmbedding = await _embeddingService.GetEmbedding($"{content} {string.Join(", ", preferences.Tags)} {preferences.MaxGroupSize}");
        var memories = await _aiMemoryRepository.SearchSimilarAsync(querryEmbedding, userId);
        var trips = await _tripRepository.SearchSimilarAsync(tripEmbedding, userId);
        var memoryContext = memories.Any()
            ? "WHAT YOU KNOW ABOUT THIS USER:\n" + string.Join("\n", memories.Select(m => $"- {m.Content}"))
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
        
        var preferencesContext = "";
        if (preferProfile)
        {
            preferencesContext = preferences != null
                ? $"\nUSER PREFERENCES:\n- Tags: {string.Join(", ", preferences.Tags)}\n- Max group size: {preferences.MaxGroupSize}"
                : "";
        }


        await _aiService.AskAsync(lastMessages, content,memoryContext,tripsContext,preferencesContext, async (chunk) =>
        {
            fullResponse += chunk;
            await Clients.Group(userId.ToString()).SendAsync("ReceiveAiChunk", chunk);
        });
        await Clients.Group(userId.ToString()).SendAsync("EndAiMessage");
        var aiResponse = new AiChatHistory
        {
            Content = fullResponse,
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
        
            string extractionPrompt = """
                                      You are a memory extraction assistant. Analyze the conversation below and extract ONLY information worth remembering about the user's travel preferences, plans, or personal facts.

                                      RULES:
                                      - Extract preferences (e.g. "prefers mountains over beach", "likes budget travel")
                                      - Extract travel plans (e.g. "wants to visit Japan next summer")
                                      - Extract personal facts relevant to travel (e.g. "travels with family", "afraid of flying")
                                      - Store the memory in the SAME LANGUAGE the user used in the conversation
                                      - If nothing is worth remembering, return empty array
                                      - Be concise — each memory should be one short sentence
                                      - Do NOT extract generic information or AI responses

                                      Conversation:
                                      User: 
                                      """ + userMessage + """Respond ONLY with valid JSON, no extra text, no markdown:{"memories": ["memory1", "memory2"]""";
            string extractedJson = "";
            extractedJson = await _aiService.ExtractAsync(extractionPrompt);
            Console.WriteLine($"Extracted JSON: {extractedJson}");

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

            Console.WriteLine($"Memories to save: {memoryTexts.Count}");

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
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
    }
}