using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Models;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Pages;
using Netptune.Handlers.Pages;
using Netptune.Handlers.Pages.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages.Commands;

public class RestorePageCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int PageId = 42;
    private const int ParentId = 50;
    private const string WorkspaceKey = "workspace";
    private const string UserId = "user-1";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly Page ArchivedPage = new()
    {
        Id = PageId,
        WorkspaceId = WorkspaceId,
        ParentId = ParentId,
        Title = "Retro",
        Slug = "retro",
        IsDeleted = true,
        CreatedByUserId = UserId,
    };
    private readonly RestorePageCommandHandler Handler;

    public RestorePageCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);

        UnitOfWork.InvokeTransaction();
        UnitOfWork.Pages.GetArchivedInWorkspace(PageId, WorkspaceId, Arg.Any<CancellationToken>()).Returns(ArchivedPage);
        UnitOfWork.Pages.GetArchivedTogetherIds(ArchivedPage, Arg.Any<CancellationToken>()).Returns([PageId, 43]);
        UnitOfWork.Pages.GetNextSortOrder(WorkspaceId, null, Arg.Any<CancellationToken>()).Returns(9d);

        UnitOfWork.Pages
            .GetLiveInWorkspace(ParentId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = ParentId, WorkspaceId = WorkspaceId, Title = "Meeting notes", Slug = "meeting-notes" });

        UnitOfWork.Pages
            .GetViewModel(PageId, WorkspaceId, UserId, Arg.Any<CancellationToken>())
            .Returns(new PageViewModel { Id = PageId, Title = "Retro", Slug = "retro" });

        GrantMember([NetptunePermissions.Pages.Delete]);

        Handler = new RestorePageCommandHandler(UnitOfWork, Identity, PermissionCache, Activity);
    }

    [Fact]
    public async Task Handle_ShouldRestoreOnlyThePagesArchivedTogether_UnderTheLiveParent()
    {
        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        ArchivedPage.ParentId.Should().Be(ParentId);
        await UnitOfWork.Pages.Received(1).Restore(
            Arg.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { PageId, 43 })),
            Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldMoveToTheRootAtTheEnd_WhenTheParentIsStillArchived()
    {
        UnitOfWork.Pages.GetLiveInWorkspace(ParentId, WorkspaceId, true, Arg.Any<CancellationToken>()).Returns((Page?)null);

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        ArchivedPage.ParentId.Should().BeNull();
        ArchivedPage.SortOrder.Should().Be(9);
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldForbid_WhenAMemberRestoresSomeoneElsesPage()
    {
        ArchivedPage.CreatedByUserId = "someone-else";

        var result = await Send();

        result.IsForbidden.Should().BeTrue();
        await UnitOfWork.Pages.DidNotReceive().Restore(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenTheRestoredSetHoldsSomeoneElsesPage()
    {
        UnitOfWork.Pages.AnyCreatedByOthers(Arg.Any<IReadOnlyCollection<int>>(), UserId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Send();

        result.Message.Should().Be(PagePermissions.OthersPagesInsideMessage);
        await UnitOfWork.Pages.DidNotReceive().Restore(Arg.Any<IEnumerable<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenThePageIsNotArchived()
    {
        UnitOfWork.Pages.GetArchivedInWorkspace(PageId, WorkspaceId, Arg.Any<CancellationToken>()).Returns((Page?)null);

        var result = await Send();

        result.IsNotFound.Should().BeTrue();
    }

    private void GrantMember(string[] permissions)
    {
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(new UserPermissions
        {
            UserId = UserId,
            WorkspaceKey = WorkspaceKey,
            Role = WorkspaceRole.Member,
            Permissions = [.. permissions],
        });
    }

    private Task<ClientResponse<PageViewModel>> Send()
    {
        return Handler.Handle(new RestorePageCommand(PageId), TestContext.Current.CancellationToken).AsTask();
    }
}
