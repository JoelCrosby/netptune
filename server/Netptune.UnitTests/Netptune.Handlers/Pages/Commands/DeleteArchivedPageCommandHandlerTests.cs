using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Models.Activity;
using Netptune.Core.Models.Search;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Pages;
using Netptune.Handlers.Pages.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pages.Commands;

public class DeleteArchivedPageCommandHandlerTests
{
    private const int WorkspaceId = 7;
    private const int PageId = 42;
    private const string WorkspaceKey = "workspace";
    private const string UserId = "user-1";

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly DeleteArchivedPageCommandHandler Handler;

    public DeleteArchivedPageCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);

        UnitOfWork.InvokeTransaction<List<int>>();
        GivenArchivedPage(UserId);
        UnitOfWork.Pages.GetSubtreeIds(PageId, WorkspaceId, true, Arg.Any<CancellationToken>()).Returns([PageId, 43]);
        UnitOfWork.Pages.DeleteArchived(Arg.Any<IReadOnlyCollection<int>>(), WorkspaceId, Arg.Any<CancellationToken>()).Returns([PageId, 43]);

        GrantMember([NetptunePermissions.Pages.Delete]);

        Handler = new DeleteArchivedPageCommandHandler(UnitOfWork, Identity, PermissionCache, Activity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldDeleteTheArchivedSubtree_AndDropItFromSearch()
    {
        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Pages.Received(1).DeleteArchived(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.SequenceEqual(new[] { PageId, 43 })),
            WorkspaceId,
            Arg.Any<CancellationToken>());
        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Delete, PageId, 43);
        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenThePageIsNotArchived()
    {
        UnitOfWork.Pages.GetArchivedInWorkspace(PageId, WorkspaceId, Arg.Any<CancellationToken>()).Returns((Page?)null);

        var result = await Send();

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.Pages.DidNotReceive().DeleteArchived(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldForbid_WhenAMemberDeletesSomeoneElsesPage()
    {
        GivenArchivedPage("someone-else");

        var result = await Send();

        result.IsForbidden.Should().BeTrue();
        await UnitOfWork.Pages.DidNotReceive().DeleteArchived(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenTheSubtreeHoldsOthersPages()
    {
        UnitOfWork.Pages.AnyCreatedByOthers(Arg.Any<IReadOnlyCollection<int>>(), UserId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Send();

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PagePermissions.OthersPagesInsideMessage);
        await UnitOfWork.Pages.DidNotReceive().DeleteArchived(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldDeleteOthersPages_WhenTheCallerMayDeleteAny()
    {
        GivenArchivedPage("someone-else");
        GrantMember([NetptunePermissions.Pages.Delete, NetptunePermissions.Pages.DeleteAny]);
        UnitOfWork.Pages.AnyCreatedByOthers(Arg.Any<IReadOnlyCollection<int>>(), UserId, Arg.Any<CancellationToken>()).Returns(true);

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
    }

    private void GivenArchivedPage(string creatorId)
    {
        UnitOfWork.Pages
            .GetArchivedInWorkspace(PageId, WorkspaceId, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = PageId, WorkspaceId = WorkspaceId, Title = "Retro", Slug = "retro", IsDeleted = true, CreatedByUserId = creatorId });
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

    private Task<ClientResponse> Send()
    {
        return Handler.Handle(new DeleteArchivedPageCommand(PageId), TestContext.Current.CancellationToken).AsTask();
    }
}
