using FluentAssertions;

using Netptune.Core.SourceControl;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.SourceControl;

public class TaskReferenceExtractorTests
{
    private readonly TaskReferenceExtractor Extractor = new(["netp", "gith", "proj-2"]);

    [Theory]
    [InlineData("feature/netp-564-search", "netp-564")]
    [InlineData("Fix paging (NETP-12)", "netp-12")]
    [InlineData("netp-7", "netp-7")]
    [InlineData("feature_gith-3_storage", "gith-3")]
    [InlineData("Closes proj-2-15", "proj-2-15")]
    public void Extract_ShouldFindTaskIdsForTheWorkspaceKeys(string text, string expected)
    {
        Extractor.Extract(text).Should().Equal(expected);
    }

    [Theory]
    [InlineData("xnetp-1")]
    [InlineData("netp-")]
    [InlineData("netp1")]
    [InlineData("utf-8 and sha-256")]
    [InlineData("other-12")]
    public void Extract_ShouldIgnoreTextThatIsNotATaskId(string text)
    {
        Extractor.Extract(text).Should().BeEmpty();
    }

    [Fact]
    public void Extract_ShouldReadTheWholeNumber()
    {
        Extractor.Extract("netp-5640").Should().Equal("netp-5640");
    }

    [Fact]
    public void Extract_ShouldDeduplicateAcrossTexts()
    {
        var found = Extractor.Extract("Fix netp-1 and gith-2", null, "feature/NETP-1-again");

        found.Should().Equal("netp-1", "gith-2");
    }

    [Fact]
    public void Extract_ShouldFindNothingWhenTheWorkspaceHasNoProjects()
    {
        new TaskReferenceExtractor([]).Extract("netp-1").Should().BeEmpty();
    }
}
