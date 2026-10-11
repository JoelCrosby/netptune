using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageImportPlanTests
{
    [Fact]
    public void Build_ShouldMakeAFolderForEachDirectory_AndNestThePagesInside()
    {
        var nodes = PageImportPlan.Build(
        [
            new PageImportFile("Engineering/RFCs/rfc-1.md", "# Offline sync\n\nBody"),
            new PageImportFile("Engineering/overview.md", "Text"),
        ]);

        var folders = nodes.Where(node => node.Kind == PageKind.Folder).ToList();
        var rfc = nodes.Single(node => node.Key == "Engineering/RFCs/rfc-1.md");

        folders.Select(folder => folder.Key).Should().Equal("Engineering", "Engineering/RFCs");
        folders.Single(folder => folder.Key == "Engineering/RFCs").ParentKey.Should().Be("Engineering");
        rfc.ParentKey.Should().Be("Engineering/RFCs");
    }

    [Fact]
    public void Build_ShouldTitleAPageByItsOpeningHeading_AndDropItFromTheBody()
    {
        var node = PageImportPlan.Build([new PageImportFile("notes.md", "# Launch plan\n\n## Steps\n\n- One")]).Single();

        node.Title.Should().Be("Launch plan");
        node.Body.Should().Be("## Steps\n\n- One");
    }

    [Fact]
    public void Build_ShouldFallBackToTheFileName_WhenThePageDoesNotOpenWithAHeading()
    {
        var node = PageImportPlan.Build([new PageImportFile("deploy-guide.md", "Intro\n\n# Later heading")]).Single();

        node.Title.Should().Be("deploy-guide");
        node.Body.Should().Contain("# Later heading");
    }

    [Fact]
    public void Build_ShouldIgnoreDotSegmentsInPaths()
    {
        var node = PageImportPlan.Build([new PageImportFile("../../etc/./notes.md", "x")]).Single(node => node.Kind == PageKind.Document);

        node.Key.Should().Be("etc/notes.md");
    }
}
