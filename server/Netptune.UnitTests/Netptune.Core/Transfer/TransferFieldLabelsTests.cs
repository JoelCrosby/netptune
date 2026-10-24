using System.Globalization;

using FluentAssertions;

using Netptune.Core.Localization;
using Netptune.Transfer;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Transfer;

public class TransferFieldLabelsTests
{
    [Fact]
    public void EveryExportableField_ShouldHaveAnEnglishHeaderMatchingItsName()
    {
        var fields = TransferFieldCatalog.All
            .Where(recordType => recordType.IsStandaloneExportable)
            .SelectMany(recordType => recordType.Fields)
            .ToList();
        var mismatched = fields
            .Where(field => EnglishHeader(field) != field.Name)
            .Select(field => field.Key)
            .ToList();

        mismatched.Should().BeEmpty("the English header is what the importer and older exports expect");
    }

    [Fact]
    public void Header_ShouldFollowTheCurrentUiCulture()
    {
        var dueDate = TransferFieldCatalog.Task.Fields.Single(field => field.Name == "Due date");

        using var culture = CultureScope.Use("de");

        TransferFieldLabels.Header(dueDate).Should().Be("Fälligkeitsdatum");
    }

    [Fact]
    public void HeadersInEveryLocale_ShouldIncludeEachTranslation()
    {
        var dueDate = TransferFieldCatalog.Task.Fields.Single(field => field.Name == "Due date");

        TransferFieldLabels.HeadersInEveryLocale(dueDate)
            .Should()
            .BeEquivalentTo("Due date", "Date d’échéance", "Fälligkeitsdatum", "Fecha de vencimiento");
    }

    private static string? EnglishHeader(TransferField field)
    {
        var key = TransferFieldLabels.KeyFor(field);

        return AppText.ResourceManager.GetString(key, CultureInfo.InvariantCulture);
    }
}
