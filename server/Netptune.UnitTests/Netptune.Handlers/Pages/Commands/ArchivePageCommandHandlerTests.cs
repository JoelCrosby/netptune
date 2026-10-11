using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Models;
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

public class ArchivePageCommandHandlerTests
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
    private readonly ArchivePageCommandHandler Handler;

    public ArchivePageCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUserId().Returns(UserId);

        UnitOfWork.Pages.GetSubtreeIds(PageId, WorkspaceId, false, Arg.Any<CancellationToken>()).Returns([PageId, 43, 44]);

        Handler = new ArchivePageCommandHandler(UnitOfWork, Identity, PermissionCache, Activity, EventPublisher);
    }

    [Fact]
    public async Task Handle_ShouldArchiveTheWholeSubtreeInOneCall_WhenAMemberArchivesTheirOwnPage()
    {
        GivenPageCreatedBy(UserId);
        GrantMember([NetptunePermissions.Pages.Delete]);

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Pages.Received(1).SoftDelete(
            Arg.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { PageId, 43, 44 })),
            UserId,
            Arg.Any<CancellationToken>());
        await EventPublisher.ReceivedPageIndexEvent(SearchIndexOperation.Delete, PageId, 43, 44);
    }

    [Fact]
    public async Task Handle_ShouldForbid_WhenAMemberArchivesSomeoneElsesPage()
    {
        GivenPageCreatedBy("someone-else");
        GrantMember([NetptunePermissions.Pages.Delete]);

        var result = await Send();

        result.IsForbidden.Should().BeTrue();
        await UnitOfWork.Pages.DidNotReceive().SoftDelete(Arg.Any<IEnumerable<int>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAllow_WhenTheCallerCanArchiveAnyPage()
    {
        GivenPageCreatedBy("someone-else");
        GrantMember([NetptunePermissions.Pages.Delete, NetptunePermissions.Pages.DeleteAny]);

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuse_WhenAMembersOwnFolderHoldsSomeoneElsesPage()
    {
        GivenPageCreatedBy(UserId);
        GrantMember([NetptunePermissions.Pages.Delete]);

        UnitOfWork.Pages
            .AnyCreatedByOthers(Arg.Is<IReadOnlyCollection<int>>(ids => ids.Contains(44)), UserId, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await Send();

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be(PagePermissions.OthersPagesInsideMessage);
        await UnitOfWork.Pages.DidNotReceive().SoftDelete(Arg.Any<IEnumerable<int>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotCheckCreators_WhenTheCallerCanArchiveAnyPage()
    {
        GivenPageCreatedBy("someone-else");
        GrantMember([NetptunePermissions.Pages.DeleteAny]);

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Pages.DidNotReceive().AnyCreatedByOthers(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenThePageIsNotLive()
    {
        UnitOfWork.Pages.GetLiveInWorkspace(PageId, WorkspaceId, true, Arg.Any<CancellationToken>()).Returns((Page?)null);

        var result = await Send();

        result.IsNotFound.Should().BeTrue();
    }

    private void GivenPageCreatedBy(string creatorId)
    {
        UnitOfWork.Pages
            .GetLiveInWorkspace(PageId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Page { Id = PageId, WorkspaceId = WorkspaceId, Title = "RFC-014", Slug = "rfc-014", CreatedByUserId = creatorId });
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
        return Handler.Handle(new ArchivePageCommand(PageId), TestContext.Current.CancellationToken).AsTask();
    }
}
