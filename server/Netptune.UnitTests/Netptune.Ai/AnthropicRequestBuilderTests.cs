using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Providers;
using Netptune.Ai.Tools;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AnthropicRequestBuilderTests
{
    [Fact]
    public void CreateMessage_ShouldReplayThinkingInPlace_WithItsSignature()
    {
        var payload = JsonDocument.Parse("""
            {"blocks":[
              {"type":"thinking","thinking":"","signature":"sig-1"},
              {"type":"text"},
              {"type":"tool_use","toolUseId":"call-1"}
            ]}
            """);

        var message = new AiChatMessage
        {
            Role = AiMessageRole.Assistant,
            Text = "Looking that up.",
            ToolCalls = [new AiToolCall { Id = "call-1", Name = "list_tasks", Arguments = JsonDocument.Parse("{}") }],
            ProviderPayload = payload,
        };

        var types = ReadBlockTypes(LastMessage(message));

        types.Should().Equal("thinking", "text", "tool_use");
    }

    [Fact]
    public void CreateMessage_ShouldSkipAToolCallThatWasDropped_AndKeepTheThinking()
    {
        var payload = JsonDocument.Parse("""
            {"blocks":[
              {"type":"redacted_thinking","data":"opaque"},
              {"type":"tool_use","toolUseId":"call-cut-off"}
            ]}
            """);

        var message = new AiChatMessage { Role = AiMessageRole.Assistant, Text = "Done.", ProviderPayload = payload };

        var types = ReadBlockTypes(LastMessage(message));

        types.Should().Equal("redacted_thinking", "text");
    }

    [Fact]
    public void CreateMessage_ShouldBuildTextAndCalls_WhenNoThinkingWasRecorded()
    {
        var message = new AiChatMessage
        {
            Role = AiMessageRole.Assistant,
            Text = "Hi",
            ToolCalls = [new AiToolCall { Id = "call-1", Name = "list_tasks", Arguments = JsonDocument.Parse("{}") }],
        };

        var types = ReadBlockTypes(LastMessage(message));

        types.Should().Equal("text", "tool_use");
    }

    [Fact]
    public void CreateTool_ShouldSendTheWholeSchema_IncludingAdditionalProperties()
    {
        var definition = new AiToolDefinition
        {
            Name = "list_tasks",
            Description = "List tasks.",
            InputSchema = AiToolSchema.Object("""{ "page": { "type": "integer" } }""", "page"),
        };

        var parameters = AnthropicRequestBuilder.Create(CreateRequest([], [definition]));

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(parameters.Tools![0]));
        var schema = json.RootElement.GetProperty("input_schema");

        schema.GetProperty("additionalProperties").GetBoolean().Should().BeFalse();
        schema.GetProperty("required")[0].GetString().Should().Be("page");
    }

    private static object LastMessage(AiChatMessage message)
    {
        var request = CreateRequest([new AiChatMessage { Role = AiMessageRole.User, Text = "hi" }, message], []);

        return AnthropicRequestBuilder.Create(request).Messages[^1];
    }

    private static AiChatRequest CreateRequest(List<AiChatMessage> messages, List<AiToolDefinition> tools)
    {
        return new AiChatRequest
        {
            Model = "claude-opus-5",
            SystemPrompt = "system",
            Messages = messages,
            Tools = tools,
            MaxOutputTokens = 1000,
        };
    }

    private static List<string> ReadBlockTypes(object message)
    {
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(message));

        return json.RootElement
            .GetProperty("content")
            .EnumerateArray()
            .Select(block => block.GetProperty("type").GetString()!)
            .ToList();
    }
}
