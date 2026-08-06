using Netptune.Transfer.Enums;
using FluentAssertions;

using Netptune.Transfer;
using Netptune.Transfer.Import;
using Netptune.Import;

using Xunit;

namespace Netptune.UnitTests.Netptune.Import;

public class ImportMappingSuggesterTests
{
    private static readonly ImportMappingSuggester Suggester = new();

    [Theory]
    [InlineData("Name", TaskFieldKeys.Name)]
    [InlineData("name", TaskFieldKeys.Name)]
    [InlineData("Summary", TaskFieldKeys.Name)]
    [InlineData("Title", TaskFieldKeys.Name)]
    [InlineData("Description", TaskFieldKeys.Description)]
    [InlineData("Notes", TaskFieldKeys.Description)]
    [InlineData("Status", TaskFieldKeys.Status)]
    [InlineData("State", TaskFieldKeys.Status)]
    [InlineData("Workflow status", TaskFieldKeys.Status)]
    [InlineData("Assignee", TaskFieldKeys.Assignees)]
    [InlineData("Assigned to", TaskFieldKeys.Assignees)]
    [InlineData("Labels", TaskFieldKeys.Tags)]
    [InlineData("Story points", TaskFieldKeys.EstimateValue)]
    [InlineData("Deadline", TaskFieldKeys.DueDate)]
    [InlineData("Due date", TaskFieldKeys.DueDate)]
    [InlineData("Date created", TaskFieldKeys.CreatedAt)]
    [InlineData("Reporter", TaskFieldKeys.CreatedBy)]
    public void Suggest_MapsAHeaderOntoTheFieldItMeans(string header, string expectedFieldKey)
    {
        var suggestion = Suggester.Suggest(TransferRecordTypes.Task, ProfileWith(header));

        suggestion.Mapping.Bindings.Should().ContainSingle()
            .Which.FieldKey.Should().Be(expectedFieldKey);
    }

    [Fact]
    public void Suggest_ScoresAnExactNameAboveASynonym()
    {
        var exact = Suggester.Suggest(TransferRecordTypes.Task, ProfileWith("Due date"));
        var synonym = Suggester.Suggest(TransferRecordTypes.Task, ProfileWith("Deadline"));

        exact.Mapping.Bindings[0].Confidence.Should().BeGreaterThan(synonym.Mapping.Bindings[0].Confidence);
    }

    [Fact]
    public void Suggest_ToleratesASmallTypoButNotAnUnrelatedHeader()
    {
        var typo = Suggester.Suggest(TransferRecordTypes.Task, ProfileWith("Descrption"));
        var unrelated = Suggester.Suggest(TransferRecordTypes.Task, ProfileWith("Cost centre"));

        typo.Mapping.Bindings.Should().ContainSingle().Which.FieldKey.Should().Be(TaskFieldKeys.Description);
        unrelated.Mapping.Bindings.Should().BeEmpty();
        unrelated.UnmappedColumns.Should().Equal(0);
    }

    [Fact]
    public void Suggest_NeverBindsOneFieldTwice()
    {
        var profile = ProfileWith("Summary", "Title", "Name");
        var suggestion = Suggester.Suggest(TransferRecordTypes.Task, profile);
        var nameBindings = suggestion.Mapping.Bindings.Where(binding => binding.FieldKey == TaskFieldKeys.Name);

        nameBindings.Should().ContainSingle("the strongest column wins and the rest stay unmapped");
        suggestion.Mapping.Bindings.Select(binding => binding.ColumnIndex).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Suggest_MarksEveryBindingAsHeuristicAndOrdersThemByColumn()
    {
        var suggestion = Suggester.Suggest(TransferRecordTypes.Task, ProfileWith("Summary", "Status", "Due date"));

        suggestion.Mapping.Bindings.Should().HaveCount(3);
        suggestion.Mapping.Bindings.Should().OnlyContain(binding => binding.Origin == ImportBindingOrigin.Heuristic);
        suggestion.Mapping.Bindings.Select(binding => binding.ColumnIndex).Should().BeInAscendingOrder();
    }

    [Fact]
    public void Suggest_UsesValueShapeToBreakATieBetweenTwoPlausibleNames()
    {
        var vocabulary = new ImportSuggestionVocabulary
        {
            StatusKeysByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["In Progress"] = "in-progress",
                ["Done"] = "done",
            },
        };
        var profile = new ImportSourceProfile
        {
            Kind = ImportSourceKind.Csv,
            HasHeaderRow = true,
            Columns =
            [
                new ImportSourceColumn
                {
                    Index = 0,
                    Name = "Column",
                    InferredType = TransferValueType.Text,
                    SampleValues = ["In Progress", "Done"],
                },
            ],
        };
        var withVocabulary = Suggester.Suggest(TransferRecordTypes.Task, profile, vocabulary);
        var withoutVocabulary = Suggester.Suggest(TransferRecordTypes.Task, profile);

        withVocabulary.Mapping.Bindings.Should().ContainSingle().Which.FieldKey.Should().Be(TaskFieldKeys.Status);
        withVocabulary.Mapping.Bindings[0].Confidence
            .Should().BeGreaterThan(withoutVocabulary.Mapping.Bindings[0].Confidence);
    }

    [Fact]
    public void Suggest_PreFillsAValueMapWhenSourceValuesRenameOntoWorkspaceStatuses()
    {
        var vocabulary = new ImportSuggestionVocabulary
        {
            StatusKeysByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["In Progress"] = "in-progress",
            },
        };
        var profile = new ImportSourceProfile
        {
            Kind = ImportSourceKind.Csv,
            HasHeaderRow = true,
            Columns =
            [
                new ImportSourceColumn
                {
                    Index = 0,
                    Name = "Status",
                    InferredType = TransferValueType.Text,
                    SampleValues = ["In Progress"],
                },
            ],
        };
        var suggestion = Suggester.Suggest(TransferRecordTypes.Task, profile, vocabulary);

        suggestion.Mapping.Bindings.Should().ContainSingle()
            .Which.ValueMap.Should().Contain("In Progress", "in-progress");
    }

    private static ImportSourceProfile ProfileWith(params string[] headers)
    {
        return new ImportSourceProfile
        {
            Kind = ImportSourceKind.Csv,
            HasHeaderRow = true,
            Columns = headers.Select((header, index) => new ImportSourceColumn
            {
                Index = index,
                Name = header,
                InferredType = TransferValueType.Text,
            }).ToList(),
        };
    }
}
