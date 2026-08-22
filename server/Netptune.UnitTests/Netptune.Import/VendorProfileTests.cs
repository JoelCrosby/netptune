using Netptune.Transfer.Enums;
using FluentAssertions;

using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Mapping;
using Netptune.Import;
using Netptune.Import.Vendors;
using Netptune.TestData;

using Xunit;
using Netptune.Core.Constants;

namespace Netptune.UnitTests.Netptune.Import;

public class VendorProfileTests
{
    private static readonly IImportVendorProfile[] Vendors =
    [
        new JiraImportVendorProfile(),
        new AsanaImportVendorProfile(),
        new TrelloImportVendorProfile(),
        new NetptuneImportVendorProfile(),
    ];

    [Theory]
    [InlineData(VendorSamples.Jira, ImportVendorProfile.Jira)]
    [InlineData(VendorSamples.Asana, ImportVendorProfile.Asana)]
    [InlineData(VendorSamples.Trello, ImportVendorProfile.Trello)]
    [InlineData(VendorSamples.Netptune, ImportVendorProfile.Netptune)]
    public async Task EachSample_IsRecognisedByExactlyItsOwnProfile(string sample, ImportVendorProfile expected)
    {
        var profile = await Profile(sample);
        var confident = Vendors
            .Select(vendor => new { vendor.Vendor, Confidence = vendor.Fingerprint(profile) })
            .Where(candidate => candidate.Confidence >= ImportMappingAdvisor.MinimumVendorConfidence)
            .ToList();

        confident.Should().ContainSingle().Which.Vendor.Should().Be(expected);
    }

    [Fact]
    public async Task JiraMapping_FoldsEveryRepeatedHeaderIntoOneBinding()
    {
        var profile = await Profile(VendorSamples.Jira);
        var mapping = new JiraImportVendorProfile().BuildMapping(profile);
        var tags = Binding(mapping, TaskFieldKeys.Tags);
        var sprint = Binding(mapping, TaskFieldKeys.Sprint);

        tags.AdditionalColumnIndexes.Should().HaveCount(1, "the sample repeats the Labels header twice");
        sprint.AdditionalColumnIndexes.Should().HaveCount(1, "the sample repeats the Sprint header twice");
        mapping.Dedupe!.KeyFieldKey.Should().Be(TaskFieldKeys.SystemId);
        mapping.Dedupe.Action.Should().Be(ImportDedupeAction.UpdateExisting);
    }

    [Fact]
    public async Task JiraMapping_ReadsBothValuesOfARepeatedHeader()
    {
        var profile = await Profile(VendorSamples.Jira);
        var mapping = new JiraImportVendorProfile().BuildMapping(profile);
        var rows = await Rows(VendorSamples.Jira, profile);
        var resolver = new ImportRowResolver(mapping, profile.Columns.Select(column => column.Name).ToList());
        var resolved = resolver.Resolve(rows[0]);

        resolved.TagValues.Should().BeEquivalentTo("backend", "urgent");
        resolved.Name.Should().Be("Fix the export fan-out");
        resolved.SourceId.Should().Be("PROJ-101");
    }

    [Fact]
    public async Task AsanaMapping_BindsSectionsToBoardGroupsAndSplitsCommaSeparatedTags()
    {
        var profile = await Profile(VendorSamples.Asana);
        var mapping = new AsanaImportVendorProfile().BuildMapping(profile);
        var rows = await Rows(VendorSamples.Asana, profile);
        var resolver = new ImportRowResolver(mapping, profile.Columns.Select(column => column.Name).ToList());
        var resolved = resolver.Resolve(rows[0]);

        Binding(mapping, TaskFieldKeys.BoardGroup).Should().NotBeNull();
        resolved.BoardGroupValue.Should().Be("In Progress");
        resolved.TagValues.Should().BeEquivalentTo("backend", "urgent");
        resolved.AssigneeValues.Should().ContainSingle().Which.Should().Be("person@acme.co.uk");
    }

    [Fact]
    public async Task TrelloMapping_BindsListsToBoardGroupsAndLabelsToTags()
    {
        var profile = await Profile(VendorSamples.Trello);
        var mapping = new TrelloImportVendorProfile().BuildMapping(profile);
        var rows = await Rows(VendorSamples.Trello, profile);
        var resolver = new ImportRowResolver(mapping, profile.Columns.Select(column => column.Name).ToList());
        var resolved = resolver.Resolve(rows[0]);

        resolved.Name.Should().Be("Fix the export fan-out");
        resolved.BoardGroupValue.Should().Be("list-2");
        resolved.TagValues.Should().BeEquivalentTo("backend", "urgent");
    }

    [Fact]
    public async Task NetptuneMapping_RoundTripsItsOwnExportHeaders()
    {
        var profile = await Profile(VendorSamples.Netptune);
        var mapping = new NetptuneImportVendorProfile().BuildMapping(profile);
        var rows = await Rows(VendorSamples.Netptune, profile);
        var resolver = new ImportRowResolver(mapping, profile.Columns.Select(column => column.Name).ToList());
        var resolved = resolver.Resolve(rows[0]);

        mapping.Bindings.Should().HaveCount(profile.Columns.Count, "our own export maps onto itself entirely");
        resolved.SourceId.Should().Be("acme-1");
        resolved.StatusValue.Should().Be("in-progress");
        resolved.TagValues.Should().BeEquivalentTo("backend", "urgent");
        resolved.DueDate.Should().Be(new DateOnly(2026, 8, 18));
    }

    [Fact]
    public async Task Advisor_PrefersAVendorProfileAndReportsWhichOneItRecognised()
    {
        var advisor = new ImportMappingAdvisor(Vendors, new ImportMappingSuggester());
        var profile = await Profile(VendorSamples.Jira);
        var suggestion = advisor.Suggest(TransferRecordTypes.Task, profile);

        suggestion.Vendor.Should().Be(ImportVendorProfile.Jira);
        suggestion.VendorConfidence.Should().BeGreaterThanOrEqualTo(ImportMappingAdvisor.MinimumVendorConfidence);
        suggestion.Mapping.Bindings.Should().OnlyContain(binding => binding.Origin == ImportBindingOrigin.Vendor);
    }

    private static ImportFieldBinding Binding(ImportMappingModel mapping, string fieldKey)
    {
        return mapping.Bindings.Single(binding => binding.FieldKey == fieldKey);
    }

    private static async Task<ImportSourceProfile> Profile(string sample)
    {
        var reader = ReaderFor(sample);

        await using var source = VendorSamples.Open(sample);

        return await reader.Profile(source, new ImportReadOptions(), TestContext.Current.CancellationToken);
    }

    private static async Task<List<ImportRow>> Rows(string sample, ImportSourceProfile profile)
    {
        var reader = ReaderFor(sample);
        var options = new ImportReadOptions
        {
            Delimiter = profile.Delimiter,
            HasHeaderRow = profile.HasHeaderRow,
        };
        var rows = new List<ImportRow>();

        await using var source = VendorSamples.Open(sample);

        await foreach (var row in reader.ReadRows(source, options, TestContext.Current.CancellationToken))
        {
            rows.Add(row);
        }

        return profile.HasHeaderRow ? rows.Skip(1).ToList() : rows;
    }

    private static IImportSourceReader ReaderFor(string sample)
    {
        if (sample.EndsWith(".json", StringComparison.Ordinal))
        {
            return new JsonImportSourceReader(false);
        }

        return new CsvImportSourceReader();
    }
}
