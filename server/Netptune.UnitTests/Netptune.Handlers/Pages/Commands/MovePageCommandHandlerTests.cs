using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Search;
using Netptune.Core.Requests.Pages;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages.Commands;

public class MovePageCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int PageId = 42;
    private const int ParentId = 50;
    private const string UserId = "user-1";
    private const string WorkspaceKey = "workspace";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly Page MovedPage = new() { Id = PageId, WorkspaceId = WorkspaceId, Title = "RFC-014", Slug = "rfc-014" };
    private readonly MovePageCommandHandler Handler;

    public MovePageCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);

        UnitOfWork.Pages.GetLiveInWorkspace(PageId, WorkspaceId, false, Arg.Any<CancellationToken>()).Returns(MovedPage);

        UnitOfWork.Pages
            .GetLiveInWorkspace(ParentId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = ParentId, WorkspaceId = WorkspaceId, Kind = PageKind.Folder, Title = "RFCs", Slug = "rfcs" });

        UnitOfWork.Pages.GetAncestorIds(ParentId, WorkspaceId, Arg.Any<CancellationToken>()).Returns([ParentId, 10]);
        UnitOfWork.Pages.GetSubtreeIds(PageId, WorkspaceId, false, Arg.Any<CancellationToken>()).Returns([PageId, 43]);

        UnitOfWork.Pages
            .GetViewModel(PageId, WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(new PageViewModel { Id = PageId, Title = "RFC-014", Slug = "rfc-014" });

        Handler = new MovePageCommandHandler(UnitOfWork, Identity, Activity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldReparentAndReorder_WhenTheParentIsValid()
    {
        var result = await Send(new MovePageRequest { ParentId = ParentId, SortOrder = 2.5 });

        result.IsSuccess.Should().BeTrue();
        MovedPage.ParentId.Should().Be(ParentId);
        MovedPage.SortOrder.Should().Be(2.5);
        MovedPage.ModifiedByUserId.Should().Be(UserId);
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Index, PageId, 43);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheNewParentIsInsideThePage()
    {
        UnitOfWork.Pages.GetAncestorIds(ParentId, WorkspaceId, Arg.Any<CancellationToken>()).Returns([ParentId, PageId]);

        var result = await Send(new MovePageRequest { ParentId = ParentId, SortOrder = 1 });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A page cannot be moved inside itself.");
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
        await EventPublisher.DidNotReceivePageIndexEvent();
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenThePageIsItsOwnParent()
    {
        UnitOfWork.Pages
            .GetLiveInWorkspace(PageId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(MovedPage);

        UnitOfWork.Pages.GetAncestorIds(PageId, WorkspaceId, Arg.Any<CancellationToken>()).Returns([PageId]);

        var result = await Send(new MovePageRequest { ParentId = PageId, SortOrder = 1 });

        result.Message.Should().Be("A page cannot be moved inside itself.");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheParentIsInAnotherSpace()
    {
        UnitOfWork.Pages
            .GetLiveInWorkspace(ParentId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = ParentId, WorkspaceId = WorkspaceId, ProjectId = 3, Title = "Mobile", Slug = "mobile" });

        var result = await Send(new MovePageRequest { ParentId = ParentId, SortOrder = 1 });

        result.Message.Should().Be("Pages can't be moved between spaces yet.");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheParentDoesNotExist()
    {
        UnitOfWork.Pages.GetLiveInWorkspace(ParentId, WorkspaceId, true, Arg.Any<CancellationToken>()).Returns((Page?)null);

        var result = await Send(new MovePageRequest { ParentId = ParentId, SortOrder = 1 });

        result.Message.Should().Be("The parent page does not exist.");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenTheSortOrderIsNotFinite()
    {
        var result = await Send(new MovePageRequest { ParentId = null, SortOrder = double.NaN });

        result.Message.Should().Be("The sort order must be a number.");
    }

    [Fact]
    public async Task Handle_ShouldNotReindex_WhenThePageIsOnlyReorderedAmongItsSiblings()
    {
        MovedPage.ParentId = ParentId;

        var result = await Send(new MovePageRequest { ParentId = ParentId, SortOrder = 7 });

        result.IsSuccess.Should().BeTrue();
        MovedPage.SortOrder.Should().Be(7);
        await EventPublisher.DidNotReceivePageIndexEvent();
    }

    [Fact]
    public async Task Handle_ShouldMoveToTheRoot_WhenNoParentIsGiven()
    {
        MovedPage.ParentId = ParentId;

        var result = await Send(new MovePageRequest { ParentId = null, SortOrder = 4 });

        result.IsSuccess.Should().BeTrue();
        MovedPage.ParentId.Should().BeNull();
    }

    private Task<ClientResponse<PageViewModel>> Send(MovePageRequest request)
    {
        return Handler.Handle(new MovePageCommand(PageId, request), TestContext.Current.CancellationToken).AsTask();
    }
}
