using FluentAssertions;

using Netptune.Ai.Execution.Reviews;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai.Reviews;

public class AiReviewProgressFeedTests
{
    private static readonly IReadOnlySet<AiReviewFindingCategory> AllCategories =
        Enum.GetValues<AiReviewFindingCategory>().ToHashSet();

    private readonly AiReviewProgressFeed Feed = new(AllCategories, new FrozenClock());

    [Fact]
    public void Observe_ShouldSendTextBeforeAToolCall_AsANote()
    {
        var events = Run(
            AiStreamEvent.Delta("Checking the endpoint "),
            AiStreamEvent.Delta("exists."),
            AiStreamEvent.ToolStarted("read_repository_file · src/app.ts", isFirstInTurn: true),
            AiStreamEvent.ToolCompleted("read_repository_file · src/app.ts", isError: true));

        events.Select(Describe).Should().Equal(
            "Note:Checking the endpoint exists.",
            "Tool:read_repository_file · src/app.ts",
            "ToolFinished:read_repository_file · src/app.ts");

        events[^1].IsError.Should().BeTrue();
        Feed.CallNotes.Should().Equal("Checking the endpoint exists.");
    }

    [Fact]
    public void Observe_ShouldGiveEveryCallInABatch_TheNoteWrittenBeforeIt_AndALaterTurnItsOwn()
    {
        Run(
            AiStreamEvent.Delta("Reading both files."),
            AiStreamEvent.ToolStarted("a", isFirstInTurn: true),
            AiStreamEvent.ToolCompleted("a", isError: false),
            AiStreamEvent.ToolStarted("b", isFirstInTurn: false),
            AiStreamEvent.ToolCompleted("b", isError: false),
            AiStreamEvent.ToolStarted("c", isFirstInTurn: true),
            AiStreamEvent.ToolCompleted("c", isError: false));

        Feed.CallNotes.Should().Equal("Reading both files.", "Reading both files.", null);
    }

    [Fact]
    public void Observe_ShouldSendTheReply_AsDrafts_NotAsANote()
    {
        var reply = """{"summary":"Ready.","findings":[{"category":"gap","title":"No criteria"}]}""";

        var events = Run(
            AiStreamEvent.Delta("All checked.\n"),
            AiStreamEvent.Delta(reply),
            new AiStreamEvent { Type = AiStreamEventType.TurnCompleted });

        events.Select(Describe).Should().Equal(
            "Note:All checked.\n",
            "Stage:writing",
            "Summary:Ready.",
            "Finding:No criteria");
    }

    [Fact]
    public void Observe_ShouldStartTheReply_AtABraceFollowedByAQuote_MidLine()
    {
        var events = Run(
            AiStreamEvent.Delta("Done. { "),
            AiStreamEvent.Delta("\"summary\":\"Ready.\",\"findings\":[]}"),
            new AiStreamEvent { Type = AiStreamEventType.TurnCompleted });

        events.Select(Describe).Should().Equal("Note:Done. ", "Stage:writing", "Summary:Ready.");
    }

    [Fact]
    public void Observe_ShouldKeepABraceInProse_AsPartOfTheNote()
    {
        var events = Run(
            AiStreamEvent.Delta("Checking the {id} route."),
            AiStreamEvent.ToolStarted("search", isFirstInTurn: true));

        events.Select(Describe).Should().Equal("Note:Checking the {id} route.", "Tool:search");
    }

    [Fact]
    public void Observe_ShouldTurnAReplyBackIntoANote_WhenTheTurnEndsInAToolCall()
    {
        var events = Run(
            AiStreamEvent.Delta("{\"plan\":\"look\"}"),
            AiStreamEvent.ToolStarted("search", isFirstInTurn: true));

        events.Select(Describe).Should().Equal(
            "Stage:writing",
            "Note:{\"plan\":\"look\"}",
            "Stage:investigating",
            "Tool:search");
    }

    [Fact]
    public void Observe_ShouldBatchSmallDeltas_AndMarkWhereAThinkingBlockStarts()
    {
        var events = Run(
            AiStreamEvent.Thinking("The task "),
            AiStreamEvent.Thinking("names an endpoint."),
            new AiStreamEvent { Type = AiStreamEventType.TurnUsage },
            AiStreamEvent.Thinking("Now the PR."));

        events.Select(Describe).Should().Equal("Thinking:The task names an endpoint.");
        events[0].IsNewBlock.Should().BeTrue();

        var rest = Feed.Complete();

        rest.Select(Describe).Should().Equal("Thinking:Now the PR.");
        rest[0].IsNewBlock.Should().BeTrue();
    }

    [Fact]
    public void Observe_ShouldFlush_OnceEnoughTextIsWaiting()
    {
        var events = Run(AiStreamEvent.Thinking(new string('a', 100)));

        events.Should().ContainSingle().Which.IsNewBlock.Should().BeTrue();
    }

    [Fact]
    public void Observe_ShouldCapTheNotesShownForOneTurn()
    {
        var events = Run(
            AiStreamEvent.Delta(new string('a', 1_000)),
            AiStreamEvent.ToolStarted("search", isFirstInTurn: true));

        events.Where(item => item.Type == AiReviewStreamEventType.Note).Sum(item => item.Text!.Length).Should().Be(600);
        Feed.CallNotes.Single()!.Length.Should().Be(280);
    }

    private List<AiReviewStreamEvent> Run(params AiStreamEvent[] streamEvents)
    {
        return streamEvents.SelectMany(Feed.Observe).ToList();
    }

    private static string Describe(AiReviewStreamEvent reviewEvent)
    {
        var detail = reviewEvent.Type switch
        {
            AiReviewStreamEventType.Stage => reviewEvent.Stage,
            AiReviewStreamEventType.Tool or AiReviewStreamEventType.ToolFinished => reviewEvent.Tool,
            _ => reviewEvent.Text,
        };

        return $"{reviewEvent.Type}:{detail}";
    }

    private sealed class FrozenClock : TimeProvider
    {
        public override long GetTimestamp()
        {
            return 0;
        }
    }
}
