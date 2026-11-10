using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Import;

using Xunit;

namespace Netptune.UnitTests.Netptune.Import;

public class ImportStatusCategoriesTests
{
    [Theory]
    [InlineData("Proposed", StatusCategory.New)]
    [InlineData("In Progress", StatusCategory.Active)]
    [InlineData("Resolved", StatusCategory.Done)]
    [InlineData("Completed", StatusCategory.Done)]
    [InlineData("Removed", StatusCategory.Inactive)]
    public void Parse_ShouldMapAzureDevOpsStateCategories(string value, StatusCategory expected)
    {
        ImportStatusCategories.Parse(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("  in progress ", StatusCategory.Active)]
    [InlineData("TO DO", StatusCategory.Todo)]
    [InlineData("backlog", StatusCategory.Backlog)]
    public void Parse_ShouldIgnoreCaseAndSurroundingWhitespace(string value, StatusCategory expected)
    {
        ImportStatusCategories.Parse(value).Should().Be(expected);
    }

    [Theory]
    [InlineData("Waiting for review")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Parse_ShouldReturnNull_ForAnythingItDoesNotRecognise(string? value)
    {
        ImportStatusCategories.Parse(value).Should().BeNull();
    }
}
