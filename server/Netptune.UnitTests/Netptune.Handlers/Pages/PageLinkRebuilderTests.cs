using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Pages;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Pages;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages;

public class PageLinkRebuilderTests
{
    private const int PageId = 42;
    private const int WorkspaceId = 7;
    private const string WorkspaceKey = "workspace";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private IReadOnlyCollection<PageLink> Stored = [];

    public PageLinkRebuilderTests()
    {
        UnitOfWork.PageLinks
            .ReplaceExtracted(PageId, Arg.Do<IReadOnlyCollection<PageLink>>(links => Stored = links), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
    }

    [Fact]
    public async Task Rebuild_ShouldLinkResolvedTasksOnly_MatchingSystemIdsWithoutCase()
    {
        UnitOfWork.Tasks
            .GetTaskViewModels(Arg.Any<IReadOnlyCollection<string>>(), WorkspaceKey, Arg.Any<CancellationToken>())
            .Returns([new TaskViewModel { Id = 10, SystemId = "NETP-1", Name = "Spike" }]);

        await Rebuild("See [[task:NETP-1]] and [[task:netp-2]].");

        Stored.Should().ContainSingle();
        Stored.Single().TargetType.Should().Be(PageLinkTargetType.Task);
        Stored.Single().TargetId.Should().Be("10");
        Stored.Single().IsExplicit.Should().BeFalse();
    }

    [Fact]
    public async Task Rebuild_ShouldDropSelfLinksNonNumericIdsAndPagesOutsideTheWorkspace()
    {
        UnitOfWork.Pages
            .GetExistingIds(Arg.Any<IReadOnlyCollection<int>>(), WorkspaceId, Arg.Any<CancellationToken>())
            .Returns([8]);

        await Rebuild("[[page:42]] [[page:abc]] [[page:8|Runbook]] [[page:9]]");

        Stored.Select(link => link.TargetId).Should().Equal("8");

        await UnitOfWork.Pages.Received(1).GetExistingIds(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { 8, 9 })),
            WorkspaceId,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rebuild_ShouldIgnoreUserMentions()
    {
        await Rebuild("Ask [[user:abc-123]].");

        Stored.Should().BeEmpty();
    }

    [Fact]
    public async Task Rebuild_ShouldClearExtractedLinksWithoutLookups_WhenTheBodyHasNoMentions()
    {
        Stored = [new PageLink { PageId = PageId, TargetId = "1" }];

        await Rebuild("Plain text now.");

        Stored.Should().BeEmpty();
        await UnitOfWork.Tasks.DidNotReceive().GetTaskViewModels(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Pages.DidNotReceive().GetExistingIds(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    private Task Rebuild(string body)
    {
        var update = new PageContentUpdate
        {
            PageId = PageId,
            WorkspaceId = WorkspaceId,
            WorkspaceKey = WorkspaceKey,
            ExpectedRevision = 3,
            Title = "RFC",
            Slug = "rfc-abcdefGHIJ12",
            Body = body,
            UserId = "user-1",
        };

        return PageLinkRebuilder.Rebuild(UnitOfWork, update, TestContext.Current.CancellationToken);
    }
}
