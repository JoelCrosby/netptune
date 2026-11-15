using FluentAssertions;

using Netptune.Ai.Execution.Reviews;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai.Reviews;

public class AiReviewDraftReaderTests
{
    private const string Reply = """
        {"summary":"Nearly ready, but \"done\" is undefined.","findings":[
          {"category":"gap","title":"No acceptance criteria","explanation":"x","suggestion":{"kind":"split","tasks":[{"name":"a","title":"nested"}]}},
          {"category":"unknown","title":"Dropped by the validator"},
          {"title":"Café é wording","category":"ambiguity"}
        ]}
        """;

    private static readonly IReadOnlySet<AiReviewFindingCategory> AllCategories =
        Enum.GetValues<AiReviewFindingCategory>().ToHashSet();

    [Fact]
    public void Read_ShouldReportTheSummaryAndEachUsableFindingOnce_AtEveryChunkBoundary()
    {
        for (var split = 1; split < Reply.Length; split++)
        {
            var reader = new AiReviewDraftReader(AllCategories);
            var events = new List<AiReviewStreamEvent>();

            events.AddRange(reader.Read(Reply[..split]));
            events.AddRange(reader.Read(Reply));
            events.AddRange(reader.Read(Reply));

            events.Should().HaveCount(4, $"the reply was cut at {split}");
            events[0].Type.Should().Be(AiReviewStreamEventType.Summary);
            events[0].Text.Should().Be("Nearly ready, but \"done\" is undefined.");
            events[1].Text.Should().Be("No acceptance criteria");
            events[1].Category.Should().Be(AiReviewFindingCategory.Gap);
            events[1].Position.Should().Be(0);
            events[2].Type.Should().Be(AiReviewStreamEventType.FindingDetail);
            events[2].Position.Should().Be(0);
            events[2].Text.Should().Be("x");
            events[3].Text.Should().Be("Café é wording");
            events[3].Category.Should().Be(AiReviewFindingCategory.Ambiguity);
            events[3].Position.Should().Be(2);
        }
    }

    [Fact]
    public void Read_ShouldNotDetailAFindingFromTheReplyBefore_WhenTheModelWroteItAgain()
    {
        var reader = new AiReviewDraftReader(AllCategories);
        var first = reader.Read("""{"findings":[{"category":"gap","title":"No acceptance criteria"}""");

        var second = reader.Read(
            """{"findings":[{"category":"scope","title":"Two changes in one task","explanation":"Split it"}]}""");

        first.Should().ContainSingle().Which.Text.Should().Be("No acceptance criteria");
        second.Should().BeEmpty();
    }

    [Fact]
    public void Read_ShouldHoldAnExplanation_UntilItsFindingHasBeenReported()
    {
        var reader = new AiReviewDraftReader(AllCategories);

        var events = reader.Read(
            """{"findings":[{"explanation":"Why it matters","category":"gap","title":"Missing"}]}""");

        events.Select(item => item.Type).Should().Equal(
            AiReviewStreamEventType.Finding,
            AiReviewStreamEventType.FindingDetail);
        events[1].Text.Should().Be("Why it matters");
        events[1].Position.Should().Be(0);
    }

    [Fact]
    public void Read_ShouldNotReportAnExplanation_ForACategoryTheSubjectDoesNotAllow()
    {
        var reader = new AiReviewDraftReader(new HashSet<AiReviewFindingCategory> { AiReviewFindingCategory.Gap });

        var events = reader.Read(
            """{"findings":[{"category":"scope","title":"Too big","explanation":"Why it matters"}]}""");

        events.Should().BeEmpty();
    }

    [Fact]
    public void Read_ShouldWaitForATitle_UntilItsStringIsComplete()
    {
        var reader = new AiReviewDraftReader(AllCategories);

        var events = reader.Read("""{"summary":"Ready.","findings":[{"category":"gap","title":"No acc""");

        events.Should().ContainSingle().Which.Type.Should().Be(AiReviewStreamEventType.Summary);
    }

    [Fact]
    public void Read_ShouldSkipProseAndAFence_BeforeTheReply()
    {
        var reader = new AiReviewDraftReader(AllCategories);

        var events = reader.Read("Here it is:\n```json\n{\"summary\":\"Ready.\",\"findings\":[]}\n```");

        events.Should().ContainSingle().Which.Text.Should().Be("Ready.");
    }

    [Fact]
    public void Read_ShouldLeaveOutCategoriesTheSubjectDoesNotAllow()
    {
        var reader = new AiReviewDraftReader(new HashSet<AiReviewFindingCategory> { AiReviewFindingCategory.Gap });

        var events = reader.Read("""{"findings":[{"category":"scope","title":"Too big"},{"category":"gap","title":"Missing"}]}""");

        events.Should().ContainSingle().Which.Text.Should().Be("Missing");
    }

    [Fact]
    public void Read_ShouldKeepWhatItReported_WhenTheReplyStopsBeingValidJson()
    {
        var reader = new AiReviewDraftReader(AllCategories);

        var events = reader.Read("""{"summary":"Ready.","findings":[{"category":"gap","title":"One"} oops""");

        events.Select(item => item.Text).Should().Equal("Ready.", "One");
    }

    [Fact]
    public void Read_ShouldStopAtTwelveFindings_LikeTheValidator()
    {
        var reader = new AiReviewDraftReader(AllCategories);
        var findings = Enumerable.Range(1, 14).Select(number => $$"""{"category":"gap","title":"Finding {{number}}"}""");

        var events = reader.Read($$"""{"findings":[{{string.Join(",", findings)}}]}""");

        events.Should().HaveCount(AiReviewValidator.MaxFindings);
    }
}
