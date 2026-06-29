using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Workspaces.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Workspaces.Commands;

public class LeaveWorkspaceCommandHandlerTests
{
    private readonly LeaveWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    private const string Key = "workspaceKey";
    private const string UserId = "userId";
    private const string OwnerId = "ownerId";

    public LeaveWorkspaceCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.GetCurrentUserEmail().Returns("user@email.com");

        Handler = new(UnitOfWork, Identity, Cache, Activity);
    }

    private static List<WorkspaceAppUser> RemovedMembers =>
        new() { AutoFixtures.WorkspaceAppUser with { User = new() { Email = "user@email.com" } } };

    [Fact]
    public async Task LeaveWorkspace_ShouldReturnSuccess_WhenMemberLeaves()
    {
        var workspace = AutoFixtures.Workspace with { OwnerId = OwnerId };

        UnitOfWork.Workspaces.GetBySlug(Key, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), workspace.Id, TestContext.Current.CancellationToken).Returns(RemovedMembers);

        var result = await Handler.Handle(new LeaveWorkspaceCommand(Key), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Users.Received(1).RemoveUsersFromWorkspace(
            Arg.Is<IEnumerable<string>>(ids => ids.Single() == UserId),
            workspace.Id,
            TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LeaveWorkspace_ShouldRemoveUserFromCache()
    {
        var workspace = AutoFixtures.Workspace with { OwnerId = OwnerId };

        UnitOfWork.Workspaces.GetBySlug(Key, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(RemovedMembers);

        await Handler.Handle(new LeaveWorkspaceCommand(Key), TestContext.Current.CancellationToken);

        var key = new WorkspaceUserKey { UserId = UserId, WorkspaceKey = Key };
        Cache.Received(1).Remove(Arg.Is<WorkspaceUserKey>(k => k == key));
    }

    [Fact]
    public async Task LeaveWorkspace_ShouldCallCompleteAsync_WhenMemberLeaves()
    {
        var workspace = AutoFixtures.Workspace with { OwnerId = OwnerId };

        UnitOfWork.Workspaces.GetBySlug(Key, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(RemovedMembers);

        await Handler.Handle(new LeaveWorkspaceCommand(Key), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task LeaveWorkspace_ShouldReturnNotFound_WhenWorkspaceMissing()
    {
        UnitOfWork.Workspaces.GetBySlug(Key, Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new LeaveWorkspaceCommand(Key), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.Users.DidNotReceive().RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveWorkspace_ShouldReturnFailure_WhenUserIsOwner()
    {
        var workspace = AutoFixtures.Workspace with { OwnerId = UserId };

        UnitOfWork.Workspaces.GetBySlug(Key, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);

        var result = await Handler.Handle(new LeaveWorkspaceCommand(Key), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.Users.DidNotReceive().RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task LeaveWorkspace_ShouldReturnFailure_WhenUserNotAMember()
    {
        var workspace = AutoFixtures.Workspace with { OwnerId = OwnerId };

        UnitOfWork.Workspaces.GetBySlug(Key, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(new List<WorkspaceAppUser>());

        var result = await Handler.Handle(new LeaveWorkspaceCommand(Key), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }
}
