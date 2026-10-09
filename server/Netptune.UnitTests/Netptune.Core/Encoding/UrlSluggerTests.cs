using FluentAssertions;

using Netptune.Core.Encoding;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Encoding;

public class UrlSluggerTests
{
    [Theory]
    [InlineData("Sprint 24", "sprint-24")]
    [InlineData("Assistant – October", "assistant-october")]
    [InlineData("  Q3: Hardening!  ", "q3-hardening")]
    [InlineData("Été rapide", "ete-rapide")]
    public void ToUrlSlug_ShouldSlugTheValue(string value, string expected)
    {
        value.ToUrlSlug().Should().Be(expected);
    }

    [Fact]
    public void ToUrlSlug_ShouldAppendAShortId_WhenAsked()
    {
        "Assistant – October".ToUrlSlug(appendUniqueId: true).Should().MatchRegex("^assistant-october-[A-Za-z0-9]{12}$");
    }

    [Fact]
    public void ToUrlSlug_ShouldNotRepeatTheShortId()
    {
        var slugs = Enumerable.Range(0, 1000).Select(_ => "Sprint 1".ToUrlSlug(appendUniqueId: true));

        slugs.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void ToUrlSlug_ShouldFitTheMaxLength_IncludingTheShortId()
    {
        var slug = string.Join(" ", Enumerable.Repeat("long name", 40)).ToUrlSlug(appendUniqueId: true, maxLength: 128);

        slug.Length.Should().BeLessThanOrEqualTo(128);
        slug.Should().MatchRegex("^long-name(-long-name)*(-long)?-[A-Za-z0-9]{12}$", "the cut never leaves a doubled hyphen");
    }

    [Fact]
    public void ToUrlSlug_ShouldLeaveShortSlugsAlone_UnderAMaxLength()
    {
        "Sprint 24".ToUrlSlug(maxLength: 128).Should().Be("sprint-24");
    }
}
