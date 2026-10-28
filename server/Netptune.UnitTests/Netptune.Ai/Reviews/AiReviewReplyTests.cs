using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Execution.Reviews;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai.Reviews;

public class AiReviewReplyTests
{
    [Fact]
    public void Parse_ShouldReadTheReview_WhenItIsFencedAndFollowedByProseWithBraces()
    {
        const string raw = """
            ```json
            {"summary":"Nearly ready.","findings":[{"category":"gap","title":"No criteria","relatedTasks":[]}]}
            ```
            Let me know if you want {more} detail.
            """;

        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply!.Summary.Should().Be("Nearly ready.");
        parsed.Reply.Findings!.Single().Title.Should().Be("No criteria");
    }

    [Fact]
    public void Parse_ShouldSkipAnEarlierObject_ThatIsNotAReview()
    {
        const string raw = """Using {"format":"json"} as asked: {"summary":"Ready.","findings":[]}""";

        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply!.Summary.Should().Be("Ready.");
    }

    [Fact]
    public void Parse_ShouldIgnoreBracesInsideStrings()
    {
        const string raw = """{"summary":"Uses } and { in text.","findings":[]}""";

        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply!.Summary.Should().Be("Uses } and { in text.");
    }

    [Fact]
    public void Parse_ShouldAcceptTrailingCommasAndComments()
    {
        const string raw = """
            {
              // the review
              "summary": "Ready.",
              "findings": [{"category": "gap", "title": "A",},],
            }
            """;

        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply!.Findings.Should().ContainSingle();
    }

    [Fact]
    public void Parse_ShouldKeepTheReview_WhenAFindingHasValuesOfTheWrongType()
    {
        const string raw = """
            {"summary":"Ready.","findings":[
              "not an object",
              {"category":"overlap","title":"Dupe","quote":["a"],"suggestion":"edit it","relatedTasks":[715,"netp-1",null]},
              {"category":"gap","title":"Kept"}
            ]}
            """;

        var parsed = AiReviewReply.Parse(raw);
        var findings = parsed.Reply!.Findings!;

        findings.Select(finding => finding.Title).Should().Equal("Dupe", "Kept");
        findings[0].Quote.Should().BeNull();
        findings[0].Suggestion.Should().BeNull();
        findings[0].RelatedTasks.Should().Equal("715", "netp-1");
    }

    [Fact]
    public void Parse_ShouldReadPropertiesCaseInsensitively_AndKeepObjectSuggestions()
    {
        const string raw = """
            {"Summary":"Ready.","Findings":[{"Category":"gap","Title":"A","Suggestion":{"kind":"edit","replace":"x"}}]}
            """;

        var parsed = AiReviewReply.Parse(raw);
        var finding = parsed.Reply!.Findings!.Single();

        parsed.Reply.Summary.Should().Be("Ready.");
        finding.Suggestion!.Value.GetProperty("kind").GetString().Should().Be("edit");
    }

    [Theory]
    [InlineData("   ", "The reply was empty.")]
    [InlineData("I could not review this.", "The reply contained no complete JSON object.")]
    [InlineData("""{"summary":"cut""", "The reply contained no complete JSON object.")]
    [InlineData("""{"verdict":"fine"}""", "The JSON object had no summary or findings.")]
    public void Parse_ShouldExplainWhy_WhenThereIsNoReview(string raw, string problem)
    {
        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply.Should().BeNull();
        parsed.Problem.Should().Be(problem);
    }

    [Fact]
    public void Parse_ShouldReportTheJsonError_WhenTheObjectIsMalformed()
    {
        const string raw = """{"summary": Ready, "findings": []}""";

        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply.Should().BeNull();
        parsed.Problem.Should().NotBeNullOrWhiteSpace();
        parsed.Problem.Should().NotBe("The reply contained no complete JSON object.");
        parsed.Problem.Should().NotBe("The JSON object had no summary or findings.");
    }

    [Fact]
    public void Parse_ShouldFindTheReview_AfterAnUnclosedBraceInTheProse()
    {
        const string raw = """Use { to open a block. {"summary":"Ready.","findings":[]}""";

        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply!.Summary.Should().Be("Ready.");
    }

    [Fact]
    public void Parse_ShouldGiveUp_AfterTooManyUnclosedBraces()
    {
        var strayBraces = string.Concat(Enumerable.Repeat("{ ", 16));
        var raw = strayBraces + """{"summary":"Ready.","findings":[]}""";

        var parsed = AiReviewReply.Parse(raw);

        parsed.Reply.Should().BeNull();
    }

    [Fact]
    public void Parse_ShouldNotTieTheSuggestionToADisposedDocument()
    {
        const string raw = """{"findings":[{"category":"gap","title":"A","suggestion":{"kind":"edit"}}]}""";

        var suggestion = AiReviewReply.Parse(raw).Reply!.Findings!.Single().Suggestion!.Value;

        suggestion.ValueKind.Should().Be(JsonValueKind.Object);
        suggestion.GetRawText().Should().Contain("edit");
    }
}
