using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Agent;
using TripGeniusBackend.Application.Interfaces.Services;
using TripGeniusBackend.Application.Settings;

namespace TripGeniusBackend.Infrastructure.Persistence.Services;

/// <summary>
/// Reusable agent orchestrator over OpenRouter chat-completions with client-side function calling.
/// Streams the model's final answer and dispatches any tool calls to the supplied <see cref="AgentTool"/>s.
/// </summary>
public class AgentRunner : IAgentRunner
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AgentRunner> _logger;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _webMaxResults;

    private const int MaxRounds = 5;

    public AgentRunner(HttpClient httpClient, IOptions<OpenRouterSettings> settings, ILogger<AgentRunner> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        var s = settings.Value;
        _apiKey = s.ApiKey;
        _model = string.IsNullOrWhiteSpace(s.AgentModel) ? s.ChatModel : s.AgentModel;
        _webMaxResults = s.ChatWebMaxResults > 0 ? s.ChatWebMaxResults : 5;
    }

    public async Task<AgentRunResult> RunAsync(AgentRequest request, Func<string, Task> onChunk, CancellationToken ct = default)
    {
        var messages = new List<object> { new { role = "system", content = request.SystemPrompt } };
        foreach (var turn in request.History)
            messages.Add(new { role = turn.Role, content = turn.Content });
        messages.Add(new { role = "user", content = request.UserMessage });

        var toolsByName = request.Tools.ToDictionary(t => t.Name, StringComparer.Ordinal);
        var toolSpecs = BuildToolSpecs(request.Tools, request.EnableWebSearch);

        var finalText = new StringBuilder();
        int toolCallCount = 0;
        bool anyToolSucceeded = false;

        for (int round = 0; round < MaxRounds; round++)
        {
            var (content, toolCalls) = await StreamRoundAsync(messages, toolSpecs, onChunk, ct);

            if (toolCalls.Count == 0)
            {
                finalText.Append(content);
                break;
            }

            // Record the assistant turn (with its tool calls) before appending the tool results.
            messages.Add(new
            {
                role = "assistant",
                content = string.IsNullOrEmpty(content) ? null : content,
                tool_calls = toolCalls.Select(tc => new
                {
                    id = tc.Id,
                    type = "function",
                    function = new { name = tc.Name, arguments = tc.Arguments }
                }).ToArray()
            });

            foreach (var call in toolCalls)
            {
                toolCallCount++;
                string result;
                if (!toolsByName.TryGetValue(call.Name, out var tool))
                {
                    result = $"Error: unknown tool '{call.Name}'.";
                }
                else
                {
                    try
                    {
                        using var argsDoc = JsonDocument.Parse(string.IsNullOrWhiteSpace(call.Arguments) ? "{}" : call.Arguments);
                        result = await tool.Handler(argsDoc.RootElement.Clone());
                        anyToolSucceeded = true;
                    }
                    catch (JsonException)
                    {
                        result = "Error: could not parse the arguments as JSON.";
                    }
                    catch (Exception ex)
                    {
                        // Feed the failure back so the model can explain it to the user instead of crashing.
                        _logger.LogInformation("Agent tool '{Tool}' failed: {Message}", call.Name, ex.Message);
                        result = $"Error: {ex.Message}";
                    }
                }

                messages.Add(new { role = "tool", tool_call_id = call.Id, content = result });
            }

            if (round == MaxRounds - 1)
                _logger.LogWarning("Agent reached max rounds ({Max}) without a final answer", MaxRounds);
        }

        return new AgentRunResult
        {
            FinalText = finalText.ToString().Trim(),
            ToolCallCount = toolCallCount,
            AnyToolSucceeded = anyToolSucceeded
        };
    }

    private sealed class PendingToolCall
    {
        public string Id = "";
        public string Name = "";
        public readonly StringBuilder Args = new();
        public string Arguments => Args.ToString();
    }

    /// <summary>Streams one completion round: emits content chunks and accumulates any tool calls.</summary>
    private async Task<(string content, List<PendingToolCall> toolCalls)> StreamRoundAsync(
        List<object> messages, object[] toolSpecs, Func<string, Task> onChunk, CancellationToken ct)
    {
        var body = new
        {
            model = _model,
            stream = true,
            messages,
            tools = toolSpecs,
            tool_choice = "auto"
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        httpRequest.Headers.Add("HTTP-Referer", "https://tripgenius.online");
        httpRequest.Headers.Add("X-Title", "TripGenius");
        httpRequest.Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(httpRequest, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);

        var content = new StringBuilder();
        var byIndex = new SortedDictionary<int, PendingToolCall>();

        while (!reader.EndOfStream)
        {
            var line = await reader.ReadLineAsync(ct);
            if (string.IsNullOrEmpty(line) || !line.StartsWith("data:")) continue;

            var json = line[5..].Trim();
            if (json == "[DONE]") break;

            JsonDocument doc;
            try { doc = JsonDocument.Parse(json); }
            catch (JsonException) { continue; }

            using (doc)
            {
                if (!doc.RootElement.TryGetProperty("choices", out var choices) || choices.GetArrayLength() == 0)
                    continue;

                var delta = choices[0].GetProperty("delta");

                if (delta.TryGetProperty("content", out var contentEl) &&
                    contentEl.ValueKind == JsonValueKind.String)
                {
                    var chunk = contentEl.GetString();
                    if (!string.IsNullOrEmpty(chunk))
                    {
                        content.Append(chunk);
                        await onChunk(chunk);
                    }
                }

                if (delta.TryGetProperty("tool_calls", out var toolCallsEl) &&
                    toolCallsEl.ValueKind == JsonValueKind.Array)
                {
                    AccumulateToolCalls(toolCallsEl, byIndex);
                }
            }
        }

        return (content.ToString(), byIndex.Values.Where(c => !string.IsNullOrEmpty(c.Name)).ToList());
    }

    private static void AccumulateToolCalls(JsonElement toolCallsEl, SortedDictionary<int, PendingToolCall> byIndex)
    {
        foreach (var tc in toolCallsEl.EnumerateArray())
        {
            var index = tc.TryGetProperty("index", out var idxEl) && idxEl.TryGetInt32(out var i) ? i : 0;
            if (!byIndex.TryGetValue(index, out var pending))
            {
                pending = new PendingToolCall();
                byIndex[index] = pending;
            }

            if (tc.TryGetProperty("id", out var idEl) && idEl.ValueKind == JsonValueKind.String)
            {
                var id = idEl.GetString();
                if (!string.IsNullOrEmpty(id)) pending.Id = id;
            }

            if (tc.TryGetProperty("function", out var fn))
            {
                if (fn.TryGetProperty("name", out var nameEl) && nameEl.ValueKind == JsonValueKind.String)
                {
                    var name = nameEl.GetString();
                    if (!string.IsNullOrEmpty(name)) pending.Name = name;
                }
                if (fn.TryGetProperty("arguments", out var argsEl) && argsEl.ValueKind == JsonValueKind.String)
                {
                    pending.Args.Append(argsEl.GetString());
                }
            }
        }
    }

    private object[] BuildToolSpecs(List<AgentTool> tools, bool enableWebSearch)
    {
        var specs = new List<object>();

        foreach (var tool in tools)
        {
            specs.Add(new
            {
                type = "function",
                function = new
                {
                    name = tool.Name,
                    description = tool.Description,
                    parameters = tool.Parameters
                }
            });
        }

        if (enableWebSearch)
        {
            specs.Add(new Dictionary<string, object>
            {
                ["type"] = "openrouter:web_search",
                ["max_results"] = _webMaxResults
            });
        }

        return specs.ToArray();
    }
}
