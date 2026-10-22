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

    [Theory]
    [InlineData("# Learning C#\n\nBody", "Learning C#")]
    [InlineData("# Release notes ##\n\nBody", "Release notes")]
    [InlineData("# Release notes #   \n\nBody", "Release notes")]
    public void Build_ShouldOnlyStripAClosingSequenceFromTheHeading(string content, string title)
    {
        var node = PageImportPlan.Build([new PageImportFile("notes.md", content)]).Single();

        node.Title.Should().Be(title);
        node.Body.Should().Be("Body");
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

    [Fact]
    public void Build_ShouldKeepBothFiles_WhenTwoUploadsShareAName()
    {
        var nodes = PageImportPlan.Build(
        [
            new PageImportFile("README.md", "First"),
            new PageImportFile("README.md", "Second"),
        ]);

        nodes.Select(node => node.Key).Should().Equal("README.md", "README.md (2)");
        nodes.Select(node => node.Body).Should().Equal("First", "Second");
    }

    [Fact]
    public void Build_ShouldKeepBothFiles_WhenTheirPathsDifferOnlyByCase()
    {
        var nodes = PageImportPlan.Build(
        [
            new PageImportFile("Docs/a.md", "Upper"),
            new PageImportFile("docs/a.md", "Lower"),
        ]);

        var folder = nodes.Single(node => node.Kind == PageKind.Folder);
        var documents = nodes.Where(node => node.Kind == PageKind.Document).ToList();

        documents.Should().HaveCount(2);
        documents.Should().OnlyContain(document => document.ParentKey == folder.Key);
        documents.Select(document => document.Key).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Build_ShouldGiveAFolderAndADocumentOfTheSameNameTheirOwnKeys()
    {
        var nodes = PageImportPlan.Build(
        [
            new PageImportFile("x.md", "File"),
            new PageImportFile("x.md/child.md", "Child"),
        ]);

        var folder = nodes.Single(node => node.Kind == PageKind.Folder);
        var file = nodes.Single(node => node.Kind == PageKind.Document && node.Body == "File");
        var child = nodes.Single(node => node.Kind == PageKind.Document && node.Body == "Child");

        nodes.Should().HaveCount(3);
        nodes.Select(node => node.Key).Should().OnlyHaveUniqueItems(node => node.ToLowerInvariant());
        folder.Key.Should().NotBe(file.Key);
        child.ParentKey.Should().Be(folder.Key);
        file.ParentKey.Should().BeNull();
    }

    [Fact]
    public void Build_ShouldSuffixAgain_WhenTheSuffixedKeyIsAlsoTaken()
    {
        var nodes = PageImportPlan.Build(
        [
            new PageImportFile("a.md", "One"),
            new PageImportFile("a.md (2)", "Two"),
            new PageImportFile("A.md", "Three"),
        ]);

        nodes.Select(node => node.Key).Should().OnlyHaveUniqueItems(node => node.ToLowerInvariant());
        nodes.Should().HaveCount(3);
    }
}
