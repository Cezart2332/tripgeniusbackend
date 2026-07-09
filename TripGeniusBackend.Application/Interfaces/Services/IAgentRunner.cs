using TripGeniusBackend.Application.Agent;

namespace TripGeniusBackend.Application.Interfaces.Services;

/// <summary>
/// A small, reusable agent orchestrator: runs the OpenRouter function-calling loop
/// (call model → execute tool_calls → feed results back → repeat) until the model
/// produces a final answer, streaming that answer through <paramref name="onChunk"/>.
/// </summary>
public interface IAgentRunner
{
    Task<AgentRunResult> RunAsync(AgentRequest request, Func<string, Task> onChunk, CancellationToken ct = default);
}
