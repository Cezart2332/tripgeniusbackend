using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using TripGeniusBackend.Application.Agent;
using TripGeniusBackend.Application.Settings;
using TripGeniusBackend.Infrastructure.Persistence.Services;
using Xunit;

namespace TripGeniusBackend.Tests.Unit.Services;

public class AgentRunnerTests
{
    /// <summary>Returns a scripted OpenRouter SSE body per call (round 1 → tool call, round 2 → final text).</summary>
    private sealed class ScriptedHandler : HttpMessageHandler
    {
        private readonly Queue<string> _bodies;
        public int Calls { get; private set; }

        public ScriptedHandler(params string[] bodies) => _bodies = new Queue<string>(bodies);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            var body = _bodies.Count > 0 ? _bodies.Dequeue() : "data: [DONE]\n\n";
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/event-stream")
            });
        }
    }

    private static AgentRunner CreateRunner(HttpMessageHandler handler)
    {
        var settings = Options.Create(new OpenRouterSettings { ApiKey = "test", AgentModel = "test/model" });
        return new AgentRunner(new HttpClient(handler), settings, NullLogger<AgentRunner>.Instance);
    }

    private static string ToolCallEvent(string name, string argsJson)
    {
        var escaped = JsonSerializer.Serialize(argsJson); // JSON-encode the arguments string
        return $"data: {{\"choices\":[{{\"delta\":{{\"tool_calls\":[{{\"index\":0,\"id\":\"call_1\",\"type\":\"function\",\"function\":{{\"name\":\"{name}\",\"arguments\":{escaped}}}}}]}}}}]}}\n\ndata: [DONE]\n\n";
    }

    private static string ContentEvent(string text) =>
        $"data: {{\"choices\":[{{\"delta\":{{\"content\":{JsonSerializer.Serialize(text)}}}}}]}}\n\ndata: [DONE]\n\n";

    [Fact]
    public async Task RunAsync_WhenModelCallsTool_ExecutesItThenReturnsFinalText()
    {
        // Round 1: the model asks to call add_activity. Round 2: it returns the final answer.
        var handler = new ScriptedHandler(
            ToolCallEvent("add_activity", "{\"day\":2,\"name\":\"Dinner\",\"type\":\"Food\"}"),
            ContentEvent("Added Dinner to day 2."));
        var runner = CreateRunner(handler);

        JsonElement? capturedArgs = null;
        var tool = new AgentTool
        {
            Name = "add_activity",
            Description = "adds an activity",
            Parameters = new { type = "object" },
            Handler = args => { capturedArgs = args; return Task.FromResult("Added \"Dinner\" to day 2."); }
        };

        var streamed = new StringBuilder();
        var result = await runner.RunAsync(
            new AgentRequest { SystemPrompt = "sys", UserMessage = "add dinner to day 2", Tools = { tool } },
            chunk => { streamed.Append(chunk); return Task.CompletedTask; });

        handler.Calls.Should().Be(2); // one round to get the tool call, one to get the final answer
        result.ToolCallCount.Should().Be(1);
        result.AnyToolSucceeded.Should().BeTrue();
        result.FinalText.Should().Be("Added Dinner to day 2.");
        streamed.ToString().Should().Be("Added Dinner to day 2.");
        capturedArgs.Should().NotBeNull();
        capturedArgs!.Value.GetProperty("day").GetInt32().Should().Be(2);
    }

    [Fact]
    public async Task RunAsync_WithNoToolCalls_ReturnsAnswerInOneRound()
    {
        var handler = new ScriptedHandler(ContentEvent("The trip has 3 days planned."));
        var runner = CreateRunner(handler);

        var result = await runner.RunAsync(
            new AgentRequest { SystemPrompt = "sys", UserMessage = "how many days?" },
            _ => Task.CompletedTask);

        handler.Calls.Should().Be(1);
        result.ToolCallCount.Should().Be(0);
        result.FinalText.Should().Be("The trip has 3 days planned.");
    }

    [Fact]
    public async Task RunAsync_WhenToolThrows_FeedsErrorBackAndStillCompletes()
    {
        var handler = new ScriptedHandler(
            ToolCallEvent("add_activity", "{\"day\":9}"),
            ContentEvent("Sorry, day 9 does not exist."));
        var runner = CreateRunner(handler);

        var tool = new AgentTool
        {
            Name = "add_activity",
            Description = "adds an activity",
            Parameters = new { type = "object" },
            Handler = _ => throw new KeyNotFoundException("No day 9 in this trip.")
        };

        var result = await runner.RunAsync(
            new AgentRequest { SystemPrompt = "sys", UserMessage = "add to day 9", Tools = { tool } },
            _ => Task.CompletedTask);

        handler.Calls.Should().Be(2);
        result.AnyToolSucceeded.Should().BeFalse();
        result.FinalText.Should().Be("Sorry, day 9 does not exist.");
    }
}
