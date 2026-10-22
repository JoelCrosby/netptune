using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PublicPageVisibilityTests
{
    [Fact]
    public void VisibleIds_ShouldIncludePublishedDocuments_AndLeaveOutDrafts()
    {
        var visible = PublicPageVisibility.VisibleIds(
        [
            Document(1, null, PageStatus.Published),
            Document(2, null, PageStatus.Draft),
        ]);

        visible.Should().BeEquivalentTo([1]);
    }

    [Fact]
    public void VisibleIds_ShouldIncludeTheFoldersLeadingToAPublishedDocument()
    {
        var visible = PublicPageVisibility.VisibleIds(
        [
            Folder(1, null),
            Folder(2, 1),
            Document(3, 2, PageStatus.Published),
        ]);

        visible.Should().BeEquivalentTo([1, 2, 3]);
    }

    [Fact]
    public void VisibleIds_ShouldLeaveOutFolders_WithNothingPublishedInside()
    {
        var visible = PublicPageVisibility.VisibleIds(
        [
            Folder(1, null),
            Document(2, 1, PageStatus.Draft),
            Folder(3, null),
        ]);

        visible.Should().BeEmpty();
    }

    [Fact]
    public void VisibleIds_ShouldLeaveOutAPublishedDocument_InsideADraftDocument()
    {
        var visible = PublicPageVisibility.VisibleIds(
        [
            Folder(1, null),
            Document(2, 1, PageStatus.Draft),
            Document(3, 2, PageStatus.Published),
        ]);

        visible.Should().BeEmpty();
    }

    [Fact]
    public void VisibleIds_ShouldIncludeNestedPublishedDocuments_UnderAPublishedDocument()
    {
        var visible = PublicPageVisibility.VisibleIds(
        [
            Document(1, null, PageStatus.Published),
            Document(2, 1, PageStatus.Published),
        ]);

        visible.Should().BeEquivalentTo([1, 2]);
    }

    [Fact]
    public void VisibleIds_ShouldStop_WhenParentsFormACycle()
    {
        var visible = PublicPageVisibility.VisibleIds(
        [
            Folder(1, 2),
            Folder(2, 1),
            Document(3, 1, PageStatus.Published),
        ]);

        visible.Should().BeEquivalentTo([1, 2, 3]);
    }

    private static PageVisibilityNode Folder(int id, int? parentId)
    {
        return new PageVisibilityNode
        {
            Id = id,
            ParentId = parentId,
            Kind = PageKind.Folder,
            Status = PageStatus.Draft,
        };
    }

    private static PageVisibilityNode Document(int id, int? parentId, PageStatus status)
    {
        return new PageVisibilityNode
        {
            Id = id,
            ParentId = parentId,
            Kind = PageKind.Document,
            Status = status,
        };
    }
}
