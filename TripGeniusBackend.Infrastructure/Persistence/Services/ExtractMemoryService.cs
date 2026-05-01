using System.Text.Json;
using Pgvector;
using TripGeniusBackend.Application.Interfaces;
using TripGeniusBackend.Application.Interfaces.Repositories;
using TripGeniusBackend.Domain.Entities;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

public class ExtractMemoryService : IExtractMemoryService
{
    private readonly IAiService _aiService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IAiMemoryRepository _memoryRepository;
    


    public ExtractMemoryService(IAiService aiService, IEmbeddingService embeddingService,
        IAiMemoryRepository memoryRepository)
    {
        _aiService = aiService;
        _embeddingService = embeddingService;
        _memoryRepository = memoryRepository;
    }

    public async Task ExtractAndSaveAsync(string userMessage, string aiResponse, int userId)
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
                                  """ + userMessage + """

                                                      AI:
                                                      """ + aiResponse + """

                                                                         Respond ONLY with valid JSON, no extra text, no markdown:
                                                                         {"memories": ["memory1", "memory2"]}
                            """;
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
                Console.WriteLine($"Saving: {text}");
                var embedding = await _embeddingService.GetEmbedding(text);
                _memoryRepository.Create(new AiMemory
                {
                    UserId = userId,
                    Content = text,
                    Embedding = new Vector(embedding),
                    MemoryType = "Preference"
                });
                await _memoryRepository.SaveChanges();
                Console.WriteLine("Saved!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}