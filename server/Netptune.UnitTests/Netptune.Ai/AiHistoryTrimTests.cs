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

    [Fact]
    public void TrimHistory_ShouldStubOldToolResults_BeforeDroppingWholeTurns()
    {
        var history = new List<AiChatMessage>
        {
            User("what is in the sprint"),
            AssistantWithToolCall("call-1"),
            ToolResult("call-1", new string('r', 400)),
            Assistant("here is the list"),
            User("move the first one"),
        };

        var trimmed = AiConversationService.TrimHistory(history, 200);

        trimmed.Should().HaveCount(5, "the intent of every turn is worth more than an old tool result body");
        trimmed[0].Text.Should().Be("what is in the sprint");
        trimmed[2].ToolResults.Single().Content.Should().NotContain("rrrr");
        trimmed[2].ToolResults.Single().ToolCallId.Should().Be("call-1");
    }

    [Fact]
    public void TrimHistory_ShouldClearRoomToSpare_SoTheNextTurnDoesNotRewriteHistoryAgain()
    {
        var history = new List<AiChatMessage>
        {
            User("first"),
            AssistantWithToolCall("call-1"),
            ToolResult("call-1", new string('r', 300)),
            AssistantWithToolCall("call-2"),
            ToolResult("call-2", new string('r', 300)),
            User("newest"),
        };

        var trimmed = AiConversationService.TrimHistory(history, 500);
        var remaining = trimmed.Sum(message => message.ToolResults.Sum(result => result.Content.Length));

        trimmed.Should().HaveCount(6);
        remaining.Should().BeLessThan(300, "stopping at the budget would rewrite history again next turn");
    }

    [Fact]
    public void TrimHistory_ShouldKeepToolResultsFromTheNewestTurn()
    {
        var history = new List<AiChatMessage>
        {
            User("older question"),
            Assistant(new string('a', 300)),
            User("newest question"),
            AssistantWithToolCall("call-1"),
            ToolResult("call-1", new string('r', 300)),
        };

        var trimmed = AiConversationService.TrimHistory(history, 400);

        trimmed.Last().ToolResults.Single().Content.Should().Be(new string('r', 300));
        trimmed[0].Text.Should().Be("newest question", "older turns go before the live one is touched");
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

    private static AiChatMessage ToolResult(string callId, string content = "result")
    {
        return new AiChatMessage
        {
            Role = AiMessageRole.Tool,
            ToolResults = [new AiToolResult { ToolCallId = callId, Content = content }],
        };
    }
}
