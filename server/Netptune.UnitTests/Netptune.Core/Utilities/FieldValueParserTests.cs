using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Utilities;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Utilities;

public class FieldValueParserTests
{
    [Theory]
    [InlineData("42", 42)]
    [InlineData("  42  ", 42)]
    [InlineData("-7", -7)]
    public void TryParseInteger_AcceptsWholeNumbers(string value, int expected)
    {
        FieldValueParser.TryParseInteger(value, out var parsed).Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Theory]
    [InlineData("1.5")]
    [InlineData("")]
    [InlineData("seven")]
    public void TryParseInteger_RejectsAnythingElse(string value)
    {
        FieldValueParser.TryParseInteger(value, out _).Should().BeFalse();
    }

    [Theory]
    [InlineData("1.5", 1.5)]
    [InlineData("8", 8)]
    [InlineData("  2.25 ", 2.25)]
    public void TryParseDecimal_AcceptsPlainNumbers(string value, double expected)
    {
        FieldValueParser.TryParseDecimal(value, out var parsed).Should().BeTrue();
        parsed.Should().Be((decimal)expected);
    }

    [Fact]
    public void TryParseDecimal_AcceptsThousandsSeparators_BecauseSpreadsheetImportsCarryThem()
    {
        FieldValueParser.TryParseDecimal("1,234.5", out var parsed).Should().BeTrue();
        parsed.Should().Be(1234.5m);
    }

    // Pinned deliberately: the catalog is invariant-culture, so a comma groups thousands rather than
    // marking a decimal. Someone typing the European "1,5" gets fifteen hundred, not one and a half.
    [Fact]
    public void TryParseDecimal_TreatsACommaAsAGroupSeparator_NotADecimalPoint()
    {
        FieldValueParser.TryParseDecimal("1,5", out var parsed).Should().BeTrue();
        parsed.Should().Be(15m);
    }

    [Theory]
    [InlineData("$5")]
    [InlineData("(5)")]
    [InlineData("five")]
    public void TryParseDecimal_RejectsCurrencyAndAccountingNegatives(string value)
    {
        FieldValueParser.TryParseDecimal(value, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParseDate_AcceptsABareDate()
    {
        FieldValueParser.TryParseDate("2026-08-21", out var parsed).Should().BeTrue();
        parsed.Should().Be(new DateOnly(2026, 8, 21));
    }

    [Fact]
    public void TryParseDate_AcceptsATimestampAndKeepsItsUtcDay()
    {
        FieldValueParser.TryParseDate("2026-08-21T23:30:00Z", out var parsed).Should().BeTrue();
        parsed.Should().Be(new DateOnly(2026, 8, 21));
    }

    [Theory]
    [InlineData("next tuesday")]
    [InlineData("")]
    public void TryParseDate_RejectsAnythingElse(string value)
    {
        FieldValueParser.TryParseDate(value, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParseTimestamp_AssumesUtcWhenNoOffsetIsGiven()
    {
        FieldValueParser.TryParseTimestamp("2026-08-21T10:00:00", out var parsed).Should().BeTrue();
        parsed.Should().Be(new DateTime(2026, 8, 21, 10, 0, 0, DateTimeKind.Utc));
    }

    [Theory]
    [InlineData("High", TaskPriority.High)]
    [InlineData("high", TaskPriority.High)]
    [InlineData("  critical ", TaskPriority.Critical)]
    public void TryParseEnum_AcceptsAMemberName(string value, TaskPriority expected)
    {
        FieldValueParser.TryParseEnum<TaskPriority>(value, out var parsed).Should().BeTrue();
        parsed.Should().Be(expected);
    }

    [Fact]
    public void TryParseEnum_AcceptsAMemberNumber()
    {
        FieldValueParser.TryParseEnum<TaskPriority>("3", out var parsed).Should().BeTrue();
        parsed.Should().Be(TaskPriority.High);
    }

    // Enum.TryParse on its own returns true here and yields an undefined TaskPriority. The IsDefined
    // guard is what stops that reaching an entity.
    [Theory]
    [InlineData("99")]
    [InlineData("-1")]
    public void TryParseEnum_RejectsNumbersTheEnumDoesNotDefine(string value)
    {
        FieldValueParser.TryParseEnum<TaskPriority>(value, out _).Should().BeFalse();
    }

    [Fact]
    public void TryParseEnum_RejectsAnUnknownName()
    {
        FieldValueParser.TryParseEnum<TaskPriority>("urgent", out _).Should().BeFalse();
    }

    [Fact]
    public void TryParseEnum_ByType_ReturnsTheUnderlyingNumber()
    {
        FieldValueParser.TryParseEnum(typeof(StatusCategory), "Done", out var parsed).Should().BeTrue();
        parsed.Should().Be((int)StatusCategory.Done);
    }
}
