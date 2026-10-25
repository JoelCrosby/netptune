using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Execution.Reviews;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai.Reviews;

public class AiReviewValidatorTests
{
    private const string Description = "Export the report.\n\nIt should be fast. It should be fast.";

    private static readonly IReadOnlySet<AiReviewFindingCategory> AllCategories =
        Enum.GetValues<AiReviewFindingCategory>().ToHashSet();

    [Fact]
    public void Validate_ShouldDropFindings_WithAnUnknownOrDisallowedCategory_OrNoTitle()
    {
        var reply = Reply(
            Finding("gap", "Kept"),
            Finding("vibes", "Unknown"),
            Finding("overlap", "Not allowed here"),
            Finding("ambiguity", "   "));

        var allowed = new HashSet<AiReviewFindingCategory>
        {
            AiReviewFindingCategory.Gap,
            AiReviewFindingCategory.Ambiguity,
        };
        var draft = AiReviewValidator.Validate(reply, Context(), allowed);

        draft.Findings.Select(finding => finding.Title).Should().Equal("Kept");
    }

    [Fact]
    public void Validate_ShouldKeepTheFinding_ButDropAnEditWhosePassageDoesNotMatchExactlyOnce()
    {
        var reply = Reply(
            Finding("ambiguity", "Missing passage", Edit("Not in the text", "x")),
            Finding("ambiguity", "Repeated passage", Edit("It should be fast.", "x")),
            Finding("gap", "Unique passage", Edit("Export the report.", "Export the report as CSV.")),
            Finding("gap", "Append", Edit(null, "## Acceptance criteria")));

        var draft = AiReviewValidator.Validate(reply, Context(), AllCategories);

        draft.Findings.Should().HaveCount(4);
        draft.Findings[0].Suggestion.Should().BeNull();
        draft.Findings[1].Suggestion.Should().BeNull();
        draft.Findings[2].Suggestion!.RootElement.GetProperty("find").GetString().Should().Be("Export the report.");
        draft.Findings[3].Suggestion!.RootElement.GetProperty("find").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public void Validate_ShouldDropAnEdit_ThatChangesNothing_OrTargetsAnotherField()
    {
        var reply = Reply(
            Finding("gap", "No change", Edit("Export the report.", "Export the report.")),
            Finding("gap", "Empty append", Edit(null, "  ")),
            Finding("gap", "Other field", Edit("Export the report.", "x", field: "name")));

        var draft = AiReviewValidator.Validate(reply, Context(), AllCategories);

        draft.Findings.Should().OnlyContain(finding => finding.Suggestion == null);
    }

    [Fact]
    public void Validate_ShouldKeepOnlyRelatedTasks_FromTheCandidates()
    {
        var finding = Finding("overlap", "Duplicates the export task") with { RelatedTasks = ["netp-12", "NOPE-1"] };
        var draft = AiReviewValidator.Validate(Reply(finding), Context(), AllCategories);

        draft.Findings.Single().RelatedTaskIds.Should().Equal(12);
    }

    [Fact]
    public void Validate_ShouldKeepTheFirstTwelveFindings_AndTruncateLongText()
    {
        var findings = Enumerable.Range(1, 15).Select(index => Finding("gap", $"Finding {index}")).ToArray();
        var longTitle = Finding("gap", new string('t', 300)) with { Explanation = new string('e', 3000) };
        var reply = Reply([longTitle, .. findings]);

        var draft = AiReviewValidator.Validate(reply, Context(), AllCategories);

        draft.Findings.Should().HaveCount(AiReviewValidator.MaxFindings);
        draft.Findings[0].Title.Should().HaveLength(AiReviewValidator.MaxTitleLength);
        draft.Findings[0].Explanation.Should().HaveLength(AiReviewValidator.MaxExplanationLength);
    }

    [Fact]
    public void Parse_ShouldReadFencedJson_AndRejectProse()
    {
        var fenced = AiReviewReply.Parse("Here you go:\n```json\n{\"summary\":\"Ready.\",\"findings\":[]}\n```");
        var prose = AiReviewReply.Parse("I could not review this task.");

        fenced!.Summary.Should().Be("Ready.");
        prose.Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldKeepASplit_OnlyWithAtLeastTwoNamedTasks()
    {
        var splitTasks = new[]
        {
            new { name = "Export CSV", description = "x" },
            new { name = "Export PDF", description = "y" },
        };
        var split = Suggestion(new { kind = "split", tasks = splitTasks });
        var single = Suggestion(new { kind = "split", tasks = new[] { new { name = "Only one", description = "x" } } });
        var reply = Reply(Finding("scope", "Split", split), Finding("scope", "Single", single));

        var draft = AiReviewValidator.Validate(reply, Context(), AllCategories);

        draft.Findings[0].Suggestion!.RootElement.GetProperty("tasks").GetArrayLength().Should().Be(2);
        draft.Findings[1].Suggestion.Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldKeepALink_OnlyToACandidate_WithAKnownCategory()
    {
        var link = Suggestion(new { kind = "link", systemId = "netp-12", category = "duplicate" });
        var unknownTask = Suggestion(new { kind = "link", systemId = "NOPE-1", category = "duplicate" });
        var dependency = Suggestion(new { kind = "link", systemId = "NETP-12", category = "blocks" });
        var reply = Reply(
            Finding("overlap", "A", link),
            Finding("overlap", "B", unknownTask),
            Finding("overlap", "C", dependency));

        var draft = AiReviewValidator.Validate(reply, Context(), AllCategories);

        draft.Findings[0].Suggestion!.RootElement.GetProperty("taskId").GetInt32().Should().Be(12);
        draft.Findings[1].Suggestion.Should().BeNull();
        draft.Findings[2].Suggestion.Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldMatchTagsIgnoringCase_AndCapNewOnes()
    {
        var tags = new[] { "bug", "Brand new", "Second", "Third", "Fourth" };
        var metadata = Suggestion(new { kind = "metadata", tags, estimateValue = 3.5, priority = "High" });
        var reply = Reply(Finding("gap", "Metadata", metadata));

        var draft = AiReviewValidator.Validate(reply, Context(), AllCategories);
        var suggestion = draft.Findings.Single().Suggestion!.RootElement;
        var suggestedTags = suggestion.GetProperty("tags").EnumerateArray().ToList();

        var suggestedNames = suggestedTags.Select(tag => tag.GetProperty("name").GetString());

        suggestedNames.Should().Equal("Bug", "Brand new", "Second", "Third");
        suggestedTags.Select(tag => tag.GetProperty("isNew").GetBoolean()).Should().Equal(false, true, true, true);
        suggestion.GetProperty("estimateValue").GetDecimal().Should().Be(3.5m);
        suggestion.GetProperty("priority").GetInt32().Should().Be((int)TaskPriority.High);
    }

    [Fact]
    public void Validate_ShouldDropMetadata_WithNothingUsable()
    {
        var nothingUsable = new
        {
            kind = "metadata",
            tags = Array.Empty<string>(),
            estimateValue = -1,
            priority = "none",
        };
        var metadata = Suggestion(nothingUsable);
        var draft = AiReviewValidator.Validate(Reply(Finding("gap", "Nothing", metadata)), Context(), AllCategories);

        draft.Findings.Single().Suggestion.Should().BeNull();
    }

    private static JsonElement Suggestion(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }

    private static AiReviewContext Context()
    {
        return new AiReviewContext
        {
            Document = "<task></task>",
            Fingerprint = new string('0', 64),
            UsagePurpose = "task_review",
            EditableFields = new Dictionary<string, string> { [TaskReviewSubject.DescriptionField] = Description },
            CandidateTasks = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase) { ["NETP-12"] = 12 },
            WorkspaceTags = ["Bug", "Feature"],
        };
    }

    private static AiReviewReply Reply(params AiReviewReplyFinding[] findings)
    {
        return new AiReviewReply { Summary = "Summary.", Findings = findings.ToList() };
    }

    private static AiReviewReplyFinding Finding(string category, string title, JsonElement? suggestion = null)
    {
        return new AiReviewReplyFinding
        {
            Category = category,
            Title = title,
            Explanation = "Because.",
            Suggestion = suggestion,
        };
    }

    private static JsonElement Edit(string? find, string replace, string field = TaskReviewSubject.DescriptionField)
    {
        var edit = new { kind = "edit", field, find, replace };

        return JsonSerializer.SerializeToElement(edit);
    }
}
