using System.Text.Json;

using FluentAssertions;

using Netptune.Ai.Execution.Reviews;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

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
    public void Validate_ShouldKeepEvidence_OnlyWhereTheRunAskedAToolForThatLocator()
    {
        var finding = Finding("gap", "No dead letter") with
        {
            Evidence =
            [
                new AiReviewReplyEvidence { Locator = " src/billing/webhook-retry.queue.ts ", Detail = "Backs off only" },
                new AiReviewReplyEvidence { Locator = "src/billing/never-opened.ts", Detail = "Invented" },
                new AiReviewReplyEvidence { Locator = "SRC/BILLING/WEBHOOK-RETRY.QUEUE.TS", Detail = "Same file again" },
                new AiReviewReplyEvidence { Locator = "webhook retry" },
            ],
        };

        var draft = AiReviewValidator.Validate(Reply(finding), Context(), AllCategories);

        var evidence = draft.Findings.Should().ContainSingle().Which.Evidence;
        evidence.Select(item => item.Locator).Should().Equal(
            "src/billing/webhook-retry.queue.ts",
            "webhook retry");
        evidence[0].Detail.Should().Be("Backs off only");
        evidence[1].Detail.Should().BeNull();
    }

    [Fact]
    public void CollectToolLocators_ShouldTakeEveryStringAtAnyDepth_TrimmedAndCaseInsensitive()
    {
        var invocations = new List<AiToolInvocationRecord>
        {
            Invocation("""{"repository":" acme/api ","paths":["src/a.ts","src/b.ts"]}"""),
            Invocation("""{"filter":{"query":"webhook retry","limit":20,"include":[" "]}}"""),
        };

        var locators = AiReviewer.CollectToolLocators(invocations);

        locators.Should().BeEquivalentTo([
            "acme/api",
            "src/a.ts",
            "src/b.ts",
            "webhook retry",
        ]);
        locators.Contains("SRC/A.TS").Should().BeTrue();
    }

    [Fact]
    public void CollectToolLocators_ShouldLeaveOutACallThatFailed()
    {
        var invocations = new List<AiToolInvocationRecord>
        {
            Invocation("""{"path":"src/read.ts"}"""),
            Invocation("""{"path":"src/missing.ts"}""", isError: true),
        };

        var locators = AiReviewer.CollectToolLocators(invocations);

        locators.Should().BeEquivalentTo(["src/read.ts"]);
    }

    [Fact]
    public void Validate_ShouldDropEvidence_CitingAFileTheToolFailedToRead()
    {
        var invocations = new List<AiToolInvocationRecord>
        {
            Invocation("""{"path":"src/missing.ts"}""", isError: true),
        };
        var context = Context() with { ToolLocators = AiReviewer.CollectToolLocators(invocations) };
        var finding = Finding("gap", "No dead letter") with
        {
            Evidence = [new AiReviewReplyEvidence { Locator = "src/missing.ts" }],
        };

        var draft = AiReviewValidator.Validate(Reply(finding), context, AllCategories);

        draft.Findings.Should().ContainSingle().Which.Evidence.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldStopAtSixPiecesOfEvidence()
    {
        var locators = Enumerable.Range(1, 8).Select(number => $"file-{number}.ts").ToList();
        var finding = Finding("gap", "Too much") with
        {
            Evidence = locators.Select(locator => new AiReviewReplyEvidence { Locator = locator }).ToList(),
        };
        var context = Context() with
        {
            ToolLocators = locators.ToHashSet(StringComparer.OrdinalIgnoreCase),
        };

        var draft = AiReviewValidator.Validate(Reply(finding), context, AllCategories);

        draft.Findings.Should().ContainSingle().Which.Evidence.Should().HaveCount(AiReviewValidator.MaxEvidence);
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

        fenced.Reply!.Summary.Should().Be("Ready.");
        prose.Reply.Should().BeNull();
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

    [Fact]
    public void Validate_ShouldMatchAnEditThatQuotesATagCloseAsTheModelSawIt()
    {
        var html = "Render <div>totals</div> on the report.";
        var context = Context() with
        {
            EditableFields = new Dictionary<string, string> { [TaskReviewSubject.DescriptionField] = html },
        };
        var seen = AiReviewPrompt.Neutralise("<div>totals</div>");
        var reply = Reply(Finding("ambiguity", "Markup", Edit(seen, "<span>totals</span>")));

        var draft = AiReviewValidator.Validate(reply, context, AllCategories);
        var edit = draft.Findings.Single().Suggestion!.RootElement;

        edit.GetProperty("find").GetString().Should().Be("<div>totals</div>");
        edit.GetProperty("replace").GetString().Should().NotContain("\u200B");
    }

    [Fact]
    public void ReferencedTaskKeys_ShouldListEveryRelatedAndLinkedTask_Once()
    {
        var link = Suggestion(new { kind = "link", systemId = "NETP-30", category = "duplicate" });
        var overlap = Finding("overlap", "Duplicates another task", link) with
        {
            RelatedTasks = ["NETP-12", " netp-30 "],
        };
        var gap = Finding("gap", "Edits only", Edit(null, "More.")) with { RelatedTasks = ["NETP-44", ""] };

        var keys = AiReviewValidator.ReferencedTaskKeys(Reply(overlap, gap));

        keys.Should().Equal("NETP-12", "netp-30", "NETP-44");
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
            ToolLocators = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "src/billing/webhook-retry.queue.ts",
                "webhook retry",
            },
        };
    }

    private static AiToolInvocationRecord Invocation(string argumentsJson, bool isError = false)
    {
        return new AiToolInvocationRecord
        {
            ToolName = "read_repository_file",
            Arguments = JsonDocument.Parse(argumentsJson),
            Result = "{}",
            IsError = isError,
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
