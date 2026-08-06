using FluentAssertions;

using Netptune.Transfer;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Transfer;

public class EntityRefTests
{
    [Fact]
    public void ToString_UsesTheCanonicalTypeValueForm()
    {
        var entityRef = new EntityRef(EntityRefTypes.Task, "acme-14");

        entityRef.ToString().Should().Be("task:acme-14");
    }

    [Theory]
    [InlineData("task:acme-14", "task", "acme-14")]
    [InlineData("board-group:acme-default-board/done", "board-group", "acme-default-board/done")]
    [InlineData("comment:acme-14#3", "comment", "acme-14#3")]
    public void TryParse_RoundTripsTheCanonicalForm(string value, string expectedType, string expectedValue)
    {
        var parsed = EntityRef.TryParse(value, out var result);

        parsed.Should().BeTrue();
        result.Type.Should().Be(expectedType);
        result.Value.Should().Be(expectedValue);
        result.ToString().Should().Be(value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("task")]
    [InlineData("task:")]
    [InlineData(":acme-14")]
    public void TryParse_RejectsMalformedValues(string? value)
    {
        var parsed = EntityRef.TryParse(value, out var result);

        parsed.Should().BeFalse();
        result.Should().Be(default(EntityRef));
    }

    [Fact]
    public void Parse_ThrowsOnMalformedValues()
    {
        var parse = () => EntityRef.Parse("not-a-ref");

        parse.Should().Throw<FormatException>();
    }

    [Fact]
    public void EntityRefTypes_AreLowerKebabCaseAndUnique()
    {
        EntityRefTypes.All
            .Should().OnlyHaveUniqueItems()
            .And.OnlyContain(type =>
                type.Length > 0 &&
                type == type.ToLowerInvariant() &&
                type.All(character => char.IsAsciiLetterLower(character) || character == '-'));
    }
}
