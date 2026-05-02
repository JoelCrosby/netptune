using AutoFixture;

using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Users.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Commands;

public class RemoveUsersFromWorkspaceCommandHandlerTests
{
    private readonly RemoveUsersFromWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspaceUserCache Cache = Substitute.For<IWorkspaceUserCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public RemoveUsersFromWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Cache, Activity);
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser>
        {
            AutoFixtures.WorkspaceAppUser with { User = new() { Email = "user@email.com" } },
        };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Emails.Should().BeEquivalentTo(new List<string> { "user@email.com" });
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ShouldRemoveUsersFromCache()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser>
        {
            AutoFixtures.WorkspaceAppUser with { User = new() { Email = "user@email.com" } },
        };

        var user = AutoFixtures.AppUserFixture
            .With(x => x.Id, "userId")
            .With(x => x.Email, "user@email.com")
            .Create();
        var users = new List<AppUser> { user };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);

        await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), TestContext.Current.CancellationToken);

        var key = new WorkspaceUserKey { UserId = user.Id, WorkspaceKey = workspaceKey };
        Cache.Received(1).Remove(Arg.Is<WorkspaceUserKey>(k => k == key));
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ReturnsFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser with { User = AutoFixtures.AppUser } };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ReturnsFailure_WhenInputEmpty()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string>()), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ReturnsFailure_WhenRemovingWorkspaceOwner()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace with { OwnerId = "userId" };
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser with { User = AutoFixtures.AppUser } };
        var users = new List<AppUser> { new() { Id = "userId", Email = "user@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);

        var result = await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task RemoveUsersFromWorkspace_ShouldCallCompleteAsync_WhenValidId()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser with { User = AutoFixtures.AppUser } };
        var users = new List<AppUser> { AutoFixtures.AppUser };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(users);
        UnitOfWork.Users.RemoveUsersFromWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), TestContext.Current.CancellationToken).Returns(workspaceAppUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), TestContext.Current.CancellationToken).Returns(workspace);

        await Handler.Handle(new RemoveUsersFromWorkspaceCommand(new List<string> { "user@email.com" }), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
