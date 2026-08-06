using System.Reflection;

using FluentAssertions;

using Netptune.Transfer;
using Netptune.Transfer.Catalog;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Transfer;

public class TransferFieldCatalogTests
{
    [Fact]
    public void RecordTypes_HaveUniqueKeysAndFields()
    {
        TransferFieldCatalog.All.Should().NotBeEmpty();
        TransferFieldCatalog.All.Select(recordType => recordType.Key).Should().OnlyHaveUniqueItems();

        foreach (var recordType in TransferFieldCatalog.All)
        {
            recordType.Name.Should().NotBeNullOrWhiteSpace();
            recordType.Fields.Should().NotBeEmpty();
            recordType.Fields.Select(field => field.Key).Should().OnlyHaveUniqueItems();
        }
    }

    [Fact]
    public void EveryFieldKey_IsPrefixedWithItsRecordTypeAndIsSnakeCase()
    {
        foreach (var recordType in TransferFieldCatalog.All)
        {
            foreach (var field in recordType.Fields)
            {
                field.Key.Should().StartWith($"{recordType.Key}.");

                var name = field.Key[(recordType.Key.Length + 1)..];

                name.Should().NotBeEmpty();
                name.Should().MatchRegex("^[a-z][a-z0-9_]*$");
                field.Name.Should().NotBeNullOrWhiteSpace();
            }
        }
    }

    [Fact]
    public void RefFields_DeclareAKnownRefTypeAndOtherFieldsDeclareNone()
    {
        var fields = TransferFieldCatalog.All.SelectMany(recordType => recordType.Fields).ToList();
        var refFields = fields.Where(field => field.ValueType == TransferValueType.Ref).ToList();
        var valueFields = fields.Where(field => field.ValueType != TransferValueType.Ref).ToList();

        refFields.Should().NotBeEmpty();
        refFields.Should().OnlyContain(field => field.RefType != null && EntityRefTypes.All.Contains(field.RefType!));
        valueFields.Should().OnlyContain(field => field.RefType == null);
    }

    [Fact]
    public void Synonyms_AreLowerCaseAndUniqueWithinTheirRecordType()
    {
        foreach (var recordType in TransferFieldCatalog.All)
        {
            recordType.Fields.SelectMany(field => field.Synonyms).Should().OnlyHaveUniqueItems(
                "the suggester scores one record type at a time, so a synonym cannot mean two of its fields");
        }

        var allSynonyms = TransferFieldCatalog.All
            .SelectMany(recordType => recordType.Fields)
            .SelectMany(field => field.Synonyms)
            .ToList();

        allSynonyms.Should().NotBeEmpty();
        allSynonyms.Should().OnlyContain(synonym => synonym == synonym.Trim().ToLowerInvariant());
        allSynonyms.Should().OnlyContain(synonym => synonym.Length > 0);
    }

    [Fact]
    public void Synonyms_DoNotShadowAnotherFieldsNameInTheSameRecordType()
    {
        foreach (var recordType in TransferFieldCatalog.All)
        {
            var namesByKey = recordType.Fields.ToDictionary(field => field.Key, field => Normalize(field.Name));

            foreach (var field in recordType.Fields)
            {
                var otherNames = namesByKey
                    .Where(entry => entry.Key != field.Key)
                    .Select(entry => entry.Value)
                    .ToHashSet();

                field.Synonyms.Select(Normalize).Should().NotIntersectWith(otherNames);
            }
        }
    }

    [Fact]
    public void ArchiveRecordTypes_CoverEveryFileTheArchiveFormatDeclares()
    {
        var definitions = ArchiveCatalog.InDependencyOrder;

        definitions.Should().NotBeEmpty();
        definitions.Select(definition => definition.Key).Should().OnlyHaveUniqueItems();
        definitions.Select(definition => definition.FileName).Should().OnlyHaveUniqueItems();
        definitions.Should().OnlyContain(definition => definition.FileName.StartsWith("data/"));
        definitions.Should().OnlyContain(definition => definition.FileName.EndsWith(".ndjson"));
    }

    [Fact]
    public void TaskRecordType_DeclaresTheImportRequiredAndDefaultExportFields()
    {
        var task = TransferFieldCatalog.FindRecordType(EntityRefTypes.Task);

        task.Should().NotBeNull();
        task.Fields.Where(field => field.IsRequiredForImport)
            .Select(field => field.Key)
            .Should().Equal("task.name");

        task.Fields.Where(field => field.IsExportedByDefault).Should().NotBeEmpty();
        task.Fields.Where(field => field.IsCollection)
            .Select(field => field.Key)
            .Should().BeEquivalentTo("task.assignees", "task.tags");
    }

    [Fact]
    public void FindRecordType_And_FindField_AreCaseInsensitiveAndTolerateMisses()
    {
        TransferFieldCatalog.FindRecordType("TASK").Should().NotBeNull();
        TransferFieldCatalog.FindRecordType("nope").Should().BeNull();
        TransferFieldCatalog.FindRecordType(null).Should().BeNull();

        TransferFieldCatalog.FindField("TASK.DUE_DATE").Should().NotBeNull();
        TransferFieldCatalog.FindField("task.nope").Should().BeNull();
        TransferFieldCatalog.FindField(" ").Should().BeNull();

        TransferFieldCatalog.IsKnownField("task.name").Should().BeTrue();
        TransferFieldCatalog.IsKnownField("task.nope").Should().BeFalse();
    }

    [Fact]
    public void TaskFieldKeys_MatchTheCatalogExactly()
    {
        var declared = typeof(TaskFieldKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToList();
        var catalogued = TransferFieldCatalog.Task.Fields.Select(field => field.Key).ToList();

        declared.Should().BeEquivalentTo(catalogued,
            "every task field needs a constant so the export record source cannot silently skip it");
    }

    private static string Normalize(string value)
    {
        return new string(value.Where(char.IsLetterOrDigit).ToArray()).ToLowerInvariant();
    }
}
