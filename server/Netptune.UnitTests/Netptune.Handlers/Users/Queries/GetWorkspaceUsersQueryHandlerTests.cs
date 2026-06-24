using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Models.Repository;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.Users.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Queries;

// Merging members with pending invites, de-duplication, sorting and pagination now all
// happen inside get_workspace_users_paged.sql (covered by integration tests). These unit
// tests verify the handler resolves the workspace and faithfully maps the paged result.
public class GetWorkspaceUsersQueryHandlerTests
{
    private readonly GetWorkspaceUsersQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetWorkspaceUsersQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    private static IPagedResult<WorkspaceUserViewModel> PagedResult(
        IList<WorkspaceUserViewModel> results,
        int currentPage,
        int pageSize,
        int rowCount)
    {
        return new PagedResult<WorkspaceUserViewModel>
        {
            Results = results,
            CurrentPage = currentPage,
            PageSize = pageSize,
            RowCount = rowCount,
            PageCount = pageSize == 0 ? 0 : (rowCount + pageSize - 1) / pageSize,
        };
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldMapPagedResult()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        var member = new WorkspaceUserViewModel { Id = "user-1", Email = "user-1@email.com", DisplayName = "User One", Role = WorkspaceRole.Admin };
        var pending = new WorkspaceUserViewModel { Email = "pending@email.com", DisplayName = "pending@email.com", Role = WorkspaceRole.Member, IsPending = true };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.Users
            .GetWorkspaceUsersPaged(workspace.Id, Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(PagedResult([member, pending], currentPage: 2, pageSize: 2, rowCount: 5));

        var result = await Handler.Handle(
            new GetWorkspaceUsersQuery(new PageRequest { Page = 2, PageSize = 2 }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().HaveCount(2);
        result.Payload.Items.Should().ContainSingle(item => item.IsPending);
        result.Payload.Page.Should().Be(2);
        result.Payload.PageSize.Should().Be(2);
        result.Payload.TotalCount.Should().Be(5);
        result.Payload.TotalPages.Should().Be(3);
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldQueryByWorkspaceIdAndPageRequest()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var pageRequest = new PageRequest { Page = 3, PageSize = 10, SortBy = "email", SortDirection = "desc" };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.Users
            .GetWorkspaceUsersPaged(Arg.Any<int>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(PagedResult([], currentPage: 3, pageSize: 10, rowCount: 0));

        await Handler.Handle(new GetWorkspaceUsersQuery(pageRequest), TestContext.Current.CancellationToken);

        await UnitOfWork.Users.Received(1).GetWorkspaceUsersPaged(workspace.Id, pageRequest, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnEmpty_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().BeEmpty();
        await UnitOfWork.Users.DidNotReceive().GetWorkspaceUsersPaged(Arg.Any<int>(), Arg.Any<PageRequest>(), Arg.Any<CancellationToken>());
    }
}
