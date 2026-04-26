using FluentAssertions;

using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Services.Users.Commands.InviteUsersToWorkspace;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Users.Commands;

public class InviteUsersToWorkspaceCommandHandlerTests
{
    private readonly InviteUsersToWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEmailService Email = Substitute.For<IEmailService>();
    private readonly IHostingService Hosting = Substitute.For<IHostingService>();
    private readonly IInviteCache InviteCache = Substitute.For<IInviteCache>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public InviteUsersToWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Email, Hosting, InviteCache, Activity);
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldReturnCorrectly_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ReturnsFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).ReturnsNull();

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ReturnsFailure_WhenInputEmpty()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string>()), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldSendEmails_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        await Email.Received(1).Send(Arg.Any<SendMultipleEmailModel>());
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldNotInvite_ExistingUsers()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { new() { Id = "userId", Email = "user@email.com" } };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        var result = await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com", "existinguser@email.com" }), CancellationToken.None);

        result.Payload?.Emails.Should().Equal(new List<string> { "user@email.com" });
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldCallCompleteAsync_WhenValidId()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var workspaceAppUsers = new List<WorkspaceAppUser> { AutoFixtures.WorkspaceAppUser };
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(workspaceAppUsers);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>()).Returns(existingUsers);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>()).Returns(workspace);

        await Handler.Handle(new InviteUsersToWorkspaceCommand(new List<string> { "user@email.com" }), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }
}
