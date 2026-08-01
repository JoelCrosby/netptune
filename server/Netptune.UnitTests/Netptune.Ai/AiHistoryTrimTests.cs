using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Execution;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiHistoryTrimTests
{
    [Fact]
    public void TrimHistory_ShouldKeepEverything_WhenWithinBudget()
    {
        var history = new List<AiChatMessage>
        {
            User("hello"),
            Assistant("hi"),
        };

        var trimmed = AiConversationService.TrimHistory(history, 1000);

        trimmed.Should().HaveCount(2);
    }

    [Fact]
    public void TrimHistory_ShouldKeepTheNewestMessages_WhenOverBudget()
    {
        var history = new List<AiChatMessage>
        {
            User(new string('a', 100)),
            Assistant(new string('b', 100)),
            User("newest"),
        };

        var trimmed = AiConversationService.TrimHistory(history, 50);

        trimmed.Should().ContainSingle();
        trimmed[0].Text.Should().Be("newest");
    }

    [Fact]
    public void TrimHistory_ShouldKeepAtLeastOneMessage_WhenTheNewestExceedsTheBudget()
    {
        var history = new List<AiChatMessage> { User(new string('a', 500)) };
        var trimmed = AiConversationService.TrimHistory(history, 10);

        trimmed.Should().ContainSingle();
    }

    [Fact]
    public void TrimHistory_ShouldDropToolResults_WhenTheirAssistantTurnWasTrimmedAway()
    {
        var history = new List<AiChatMessage>
        {
            User(new string('a', 400)),
            AssistantWithToolCall("call-1"),
            ToolResult("call-1"),
            Assistant("done"),
        };

        var trimmed = AiConversationService.TrimHistory(history, 30);

        trimmed.Should().BeEmpty("a tool result without its assistant turn is rejected by the provider");
    }

    [Fact]
    public void TrimHistory_ShouldStartAtAUserMessage_WhenTrimmingLandsMidTurn()
    {
        var history = new List<AiChatMessage>
        {
            User(new string('a', 400)),
            AssistantWithToolCall("call-1"),
            ToolResult("call-1"),
            User("follow up"),
            Assistant("answer"),
        };

        var trimmed = AiConversationService.TrimHistory(history, 40);

        trimmed.Should().HaveCount(2);
        trimmed[0].Role.Should().Be(AiMessageRole.User);
        trimmed[0].Text.Should().Be("follow up");
    }

    private static AiChatMessage User(string text)
    {
        return new AiChatMessage { Role = AiMessageRole.User, Text = text };
    }

    private static AiChatMessage Assistant(string text)
    {
        return new AiChatMessage { Role = AiMessageRole.Assistant, Text = text };
    }

    private static AiChatMessage AssistantWithToolCall(string callId)
    {
        return new AiChatMessage
        {
            Role = AiMessageRole.Assistant,
            ToolCalls =
            [
                new AiToolCall
                {
                    Id = callId,
                    Name = "search_tasks",
                    Arguments = JsonDocument.Parse("{}"),
                },
            ],
        };
    }

    private static AiChatMessage ToolResult(string callId)
    {
        return new AiChatMessage
        {
            Role = AiMessageRole.Tool,
            ToolResults = [new AiToolResult { ToolCallId = callId, Content = "result" }],
        };
    }
}
