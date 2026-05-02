using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Models;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Users.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Commands;

public class ToggleUserPermissionCommandHandlerTests
{
    private readonly ToggleUserPermissionCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache WorkspacePermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public ToggleUserPermissionCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, WorkspacePermissionCache, Activity);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldAddPermission_WhenNotAlreadyGranted()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        const string permission = "tasks.read";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false, TestContext.Current.CancellationToken).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = permission };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().Contain(permission);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldRemovePermission_WhenAlreadyGranted()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        const string permission = "tasks.read";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [permission],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false, TestContext.Current.CancellationToken).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = permission };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().NotContain(permission);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var request = new ToggleUserPermissionRequest { UserId = "userId", Permission = "tasks.read" };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldReturnFailure_WhenUserNotInWorkspace()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(Arg.Any<string>(), workspaceKey, false, TestContext.Current.CancellationToken).ReturnsNull();

        var request = new ToggleUserPermissionRequest { UserId = "userId", Permission = "tasks.read" };
        var result = await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldCallSetUserPermissions_WhenSuccessful()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false, TestContext.Current.CancellationToken).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = "tasks.read" };
        await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        await UnitOfWork.WorkspaceUsers.Received(1).SetUserPermissions(userId, workspace.Id, Arg.Any<IEnumerable<string>>(), TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldCallCompleteAsync_WhenSuccessful()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false, TestContext.Current.CancellationToken).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = "tasks.read" };
        await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task ToggleUserPermission_ShouldClearPermissionCache_WhenSuccessful()
    {
        const string workspaceKey = "workspaceKey";
        const string userId = "userId";
        var workspace = AutoFixtures.Workspace;
        var userPermissions = new UserPermissions
        {
            UserId = userId, WorkspaceKey = workspaceKey, Role = WorkspaceRole.Member, Permissions = [],
        };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(userId, workspaceKey, false, TestContext.Current.CancellationToken).Returns(userPermissions);

        var request = new ToggleUserPermissionRequest { UserId = userId, Permission = "tasks.read" };
        await Handler.Handle(new ToggleUserPermissionCommand(request), CancellationToken.None);

        var expectedKey = new WorkspaceUserKey { UserId = userId, WorkspaceKey = workspaceKey };
        WorkspacePermissionCache.Received(1).Remove(Arg.Is<WorkspaceUserKey>(k => k == expectedKey));
    }
}
