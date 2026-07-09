using System.Text.Json;

namespace TripGeniusBackend.Application.Agent;

/// <summary>
/// A single capability the agent can invoke — the .NET analogue of a LangChain tool.
/// The handler receives the model's JSON arguments and returns a short textual result
/// that is fed back into the conversation.
/// </summary>
public sealed class AgentTool
{
    public required string Name { get; init; }
    public required string Description { get; init; }

    /// <summary>JSON-schema object describing the parameters (OpenAI function-calling format).</summary>
    public required object Parameters { get; init; }

    /// <summary>Executes the tool with the model-supplied arguments; returns a result string.</summary>
    public required Func<JsonElement, Task<string>> Handler { get; init; }
}

/// <summary>A prior conversation turn given to the agent for context.</summary>
public sealed class AgentMessage
{
    /// <summary>"user" or "assistant".</summary>
    public required string Role { get; init; }
    public required string Content { get; init; }
}

/// <summary>Input for one agent run.</summary>
public sealed class AgentRequest
{
    public required string SystemPrompt { get; init; }
    public required string UserMessage { get; init; }
    /// <summary>Recent conversation turns, oldest first, so the agent can hold a multi-turn dialogue.</summary>
    public List<AgentMessage> History { get; init; } = new();
    public List<AgentTool> Tools { get; init; } = new();
    public bool EnableWebSearch { get; init; }
}

/// <summary>Outcome of an agent run.</summary>
public sealed class AgentRunResult
{
    public string FinalText { get; init; } = string.Empty;
    public int ToolCallCount { get; init; }
    /// <summary>True if at least one tool ran successfully (used to signal the UI to refresh).</summary>
    public bool AnyToolSucceeded { get; init; }
}
