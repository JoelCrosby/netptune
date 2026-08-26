using System.Reflection;

using FluentAssertions;

using Netptune.Core.Constants;
using Netptune.Query.Schema;
using Netptune.Query.Tasks;
using Netptune.Transfer;

using Xunit;

namespace Netptune.UnitTests.Netptune.Query.Tasks;

public class TaskFieldCatalogTests
{
    [Fact]
    public void Fields_HaveUniqueSnakeCaseKeysUnderTheTaskPrefix()
    {
        TaskFieldCatalog.Instance.Fields.Should().NotBeEmpty();
        TaskFieldCatalog.Instance.Fields.Select(field => field.Key).Should().OnlyHaveUniqueItems();

        foreach (var field in TaskFieldCatalog.Instance.Fields)
        {
            field.Key.Should().StartWith("task.");
            field.Key[5..].Should().MatchRegex("^[a-z][a-z0-9_]*$");
            field.Name.Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void EveryField_DeclaresAtLeastOneOperatorAndACompiler()
    {
        foreach (var field in TaskFieldCatalog.Instance.Fields)
        {
            field.Operators.Should().NotBeEmpty($"{field.Key} must be usable");
            field.Operators.Should().OnlyHaveUniqueItems();
            field.Compiler.Should().NotBeNull($"{field.Key} must be compilable");
        }
    }

    [Fact]
    public void EnumFields_DeclareAnOptionSource()
    {
        var enumFields = TaskFieldCatalog.Instance.Fields.Where(field => field.ValueType == QueryValueType.Enum);

        foreach (var field in enumFields)
        {
            field.OptionSource.Should().NotBeNullOrWhiteSpace($"{field.Key} is picked from a list");
        }
    }

    [Fact]
    public void MultiValuedFields_AreCollections()
    {
        var multiValued = TaskFieldCatalog.Instance.Fields.Where(field => field.IsMultiValued);

        multiValued.Should().OnlyContain(field => field.ValueType == QueryValueType.Collection);
    }

    // Neither catalog may invent a key outside the shared vocabulary — that is what keeps an exported
    // column and a queryable field the same concept.
    [Fact]
    public void BothTaskCatalogs_OnlyUseKeysDeclaredInTheSharedVocabulary()
    {
        var declared = typeof(TaskFieldKeys)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
            .ToHashSet();

        var queryKeys = TaskFieldCatalog.Instance.Fields.Select(field => field.Key);
        var transferKeys = TransferFieldCatalog.Task.Fields.Select(field => field.Key);

        declared.Should().Contain(queryKeys);
        declared.Should().Contain(transferKeys);
    }

    [Fact]
    public void Find_IsCaseInsensitiveAndTrims()
    {
        TaskFieldCatalog.Instance.Find("  TASK.DUE_DATE ")!.Key.Should().Be(TaskFieldKeys.DueDate);
        TaskFieldCatalog.Instance.Find("task.nope").Should().BeNull();
        TaskFieldCatalog.Instance.Find(null).Should().BeNull();
    }
}
