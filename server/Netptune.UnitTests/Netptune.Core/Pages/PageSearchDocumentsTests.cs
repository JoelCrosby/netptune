using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.Models.Search;

using Xunit;

namespace Netptune.UnitTests.Netptune.Core.Pages;

public class PageSearchDocumentsTests
{
    private const string WorkspaceSlug = "acme";

    [Fact]
    public void Build_ShouldJoinTheFolderTitlesAboveThePage_OutermostFirst()
    {
        var outline = new[]
        {
            new PageOutlineEntry(1, null, "Engineering"),
            new PageOutlineEntry(2, 1, "RFCs"),
            new PageOutlineEntry(3, 2, "RFC-014"),
        };

        var documents = PageSearchDocuments.Build([Source(3, parentId: 2)], outline, WorkspaceSlug);

        var document = documents.Should().ContainSingle().Subject;
        document.Id.Should().Be("page_3");
        document.Path.Should().Be("Engineering / RFCs");
        document.WorkspaceSlug.Should().Be(WorkspaceSlug);
        document.Kind.Should().Be("Document");
        document.Body.Should().Be("Body text");
    }

    [Fact]
    public void Build_ShouldLeaveThePathEmpty_ForAPageAtTheRoot()
    {
        var outline = new[] { new PageOutlineEntry(1, null, "Handbook") };

        var documents = PageSearchDocuments.Build([Source(1, parentId: null)], outline, WorkspaceSlug);

        documents.Single().Path.Should().BeNull();
    }

    [Fact]
    public void Build_ShouldStopWalking_WhenTheParentsFormALoop()
    {
        var outline = new[]
        {
            new PageOutlineEntry(1, 2, "A"),
            new PageOutlineEntry(2, 1, "B"),
        };

        var documents = PageSearchDocuments.Build([Source(3, parentId: 1)], outline, WorkspaceSlug);

        documents.Single().Path.Should().Be("B / A");
    }

    [Fact]
    public void Build_ShouldStopAtAnArchivedParent_ThatIsNotInTheOutline()
    {
        var outline = new[] { new PageOutlineEntry(2, 9, "RFCs") };

        var documents = PageSearchDocuments.Build([Source(3, parentId: 2)], outline, WorkspaceSlug);

        documents.Single().Path.Should().Be("RFCs");
    }

    private static PageSearchSource Source(int id, int? parentId)
    {
        return new PageSearchSource
        {
            Id = id,
            ParentId = parentId,
            Kind = PageKind.Document,
            Title = $"Page {id}",
            Slug = $"page-{id}",
            Body = "Body text",
            Status = PageStatus.Draft,
            Tags = ["design"],
            UpdatedAt = new DateTime(2026, 10, 11, 9, 0, 0, DateTimeKind.Utc),
        };
    }
}
