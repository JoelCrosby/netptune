using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Execution;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Handlers.Ai.Queries;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiQuestionPersistenceTests
{
    private static readonly Guid QuestionId = Guid.Parse("2f8f4f7c-6a1f-4e2a-9b0f-2c4d5e6f7a8b");

    [Fact]
    public void Content_ShouldRoundTripAQuestion()
    {
        var question = new AiQuestion
        {
            Id = QuestionId,
            Text = "Which project should this go in?",
            Header = "Project",
            MultiSelect = true,
            Options =
            [
                new AiQuestionOption { Label = "Apollo", Description = "The customer-facing app" },
                new AiQuestionOption { Label = "Internal tools" },
            ],
        };

        var message = new AiChatMessage { Role = AiMessageRole.Assistant, Text = "hi", Question = question };
        var stored = AiMessageContent.FromChatMessage(message).ToJsonDocument();
        var restored = AiMessageContent.FromJsonDocument(stored).ToChatMessage(AiMessageRole.Assistant);

        restored.Question.Should().BeEquivalentTo(question);
    }

    [Fact]
    public void Content_ShouldRoundTripAnAnswer()
    {
        var answer = new AiQuestionAnswer
        {
            QuestionId = QuestionId,
            SelectedLabels = ["Apollo"],
        };

        var message = new AiChatMessage { Role = AiMessageRole.User, Text = "Apollo", Answer = answer };
        var stored = AiMessageContent.FromChatMessage(message).ToJsonDocument();
        var restored = AiMessageContent.FromJsonDocument(stored).ToChatMessage(AiMessageRole.User);

        restored.Answer.Should().BeEquivalentTo(answer);
    }

    [Fact]
    public void Content_ShouldReadTurnsStoredBeforeQuestionsExisted()
    {
        var stored = JsonDocument.Parse("""{"text":"hello","toolCalls":[],"toolResults":[]}""");
        var content = AiMessageContent.FromJsonDocument(stored);

        content.Text.Should().Be("hello");
        content.Question.Should().BeNull();
        content.Answer.Should().BeNull();
        content.ToolsRun.Should().BeEmpty();
    }

    [Fact]
    public void Describe_ShouldNameTheChosenOptions()
    {
        var question = new AiQuestion { Id = QuestionId, Text = "Which project?" };
        var answer = new AiQuestionAnswer
        {
            QuestionId = QuestionId,
            SelectedLabels = ["Apollo", "Internal tools"],
        };

        answer.Describe(question).Should().Be("Answering “Which project?”: Apollo, Internal tools");
    }

    [Fact]
    public void Describe_ShouldSayWhenTheUserWroteTheirOwnAnswer()
    {
        var question = new AiQuestion { Id = QuestionId, Text = "Which project?" };
        var answer = new AiQuestionAnswer { QuestionId = QuestionId, Text = "  the internal tools backlog " };

        answer.Describe(question).Should().Be(
            "Answering “Which project?” in their own words: the internal tools backlog");
    }

    [Fact]
    public void Mapper_ShouldProjectTheQuestionAndTheAnswer()
    {
        var question = new AiQuestion
        {
            Id = QuestionId,
            Text = "Which project?",
            Options = [new AiQuestionOption { Label = "Apollo" }],
        };

        var answer = new AiQuestionAnswer { QuestionId = QuestionId, SelectedLabels = ["Apollo"] };
        var asked = CreateMessage(1, AiMessageRole.Assistant, new AiMessageContent { Question = question });
        var answered = CreateMessage(2, AiMessageRole.User, new AiMessageContent { Answer = answer });
        var models = AiMessageMapper.ToViewModels([asked, answered], new Dictionary<long, List<AiEntityReference>>(), []);

        models[0].Question!.Text.Should().Be("Which project?");
        models[1].Answer!.SelectedLabels.Should().Equal("Apollo");
    }

    [Fact]
    public void Mapper_ShouldReadToolNames_FromTurnsStoredBeforeTheyWereRecorded()
    {
        var content = new AiMessageContent
        {
            ToolCalls =
            [
                new AiMessageContentToolCall { Id = "call-1", Name = "list_projects", Arguments = "{}" },
            ],
        };

        var message = CreateMessage(1, AiMessageRole.Assistant, content);
        var models = AiMessageMapper.ToViewModels([message], new Dictionary<long, List<AiEntityReference>>(), []);

        models[0].ToolNames.Should().Equal("list_projects");
    }

    [Fact]
    public void DropUnansweredToolCalls_ShouldStripACallNothingAnswers()
    {
        var history = new List<AiChatMessage>
        {
            new() { Role = AiMessageRole.User, Text = "hello" },
            new()
            {
                Role = AiMessageRole.Assistant,
                Text = "looking",
                ToolCalls = [CreateCall("call-1")],
            },
        };

        var replayable = AiConversationService.DropUnansweredToolCalls(history);

        replayable[1].ToolCalls.Should().BeEmpty("the provider rejects a tool_use with no tool_result beside it");
        replayable[1].Text.Should().Be("looking");
    }

    [Fact]
    public void DropUnansweredToolCalls_ShouldKeepACallItsResultsAnswer()
    {
        var history = new List<AiChatMessage>
        {
            new()
            {
                Role = AiMessageRole.Assistant,
                ToolCalls = [CreateCall("call-1")],
            },
            new()
            {
                Role = AiMessageRole.Tool,
                ToolResults = [new AiToolResult { ToolCallId = "call-1", Content = "[]" }],
            },
        };

        var replayable = AiConversationService.DropUnansweredToolCalls(history);

        replayable[0].ToolCalls.Should().HaveCount(1);
    }

    private static AiToolCall CreateCall(string id)
    {
        return new AiToolCall { Id = id, Name = "list_projects", Arguments = JsonDocument.Parse("{}") };
    }

    private static AiMessage CreateMessage(long id, AiMessageRole role, AiMessageContent content)
    {
        return new AiMessage
        {
            Id = id,
            Sequence = (int)id,
            Role = role,
            Content = content.ToJsonDocument(),
            Model = "test-model",
            CreatedAt = new DateTime(2026, 8, 10, 12, 0, 0, DateTimeKind.Utc),
        };
    }
}
