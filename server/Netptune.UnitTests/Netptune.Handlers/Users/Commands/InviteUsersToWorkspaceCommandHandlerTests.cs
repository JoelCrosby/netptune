using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Messaging;
using Netptune.Core.Models.Messaging;
using Netptune.Core.Relationships;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Users.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Commands;

public class InviteUsersToWorkspaceCommandHandlerTests
{
    private readonly InviteUsersToWorkspaceCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IEmailService Email = Substitute.For<IEmailService>();
    private readonly IHostingService Hosting = Substitute.For<IHostingService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public InviteUsersToWorkspaceCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Email, Hosting, Activity);
    }

    private void SetupValidWorkspace(string workspaceKey, Workspace workspace,
        List<AppUser> users, List<AppUser> existingUsers,
        List<WorkspaceInvite>? pendingInvites = null)
    {
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);
        UnitOfWork.Users.GetByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(users);
        UnitOfWork.Users.IsUserInWorkspaceRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(existingUsers);
        UnitOfWork.Users.InviteUsersToWorkspace(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns([]);
        UnitOfWork.WorkspaceInvites.GetPendingByEmailRange(Arg.Any<IEnumerable<string>>(), Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(pendingInvites ?? []);
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldReturnSuccess_WhenInputValid()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var users = new List<AppUser> { AutoFixtures.AppUser };
        var existingUsers = new List<AppUser> { new() { Id = "userId", Email = "existinguser@email.com" } };

        SetupValidWorkspace(workspaceKey, workspace, users, existingUsers);

        var result = await Handler.Handle(
            new InviteUsersToWorkspaceCommand(["user@email.com"]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ReturnsFailure_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Handler.Handle(
            new InviteUsersToWorkspaceCommand(["user@email.com"]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ReturnsFailure_WhenInputEmpty()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace);

        var result = await Handler.Handle(
            new InviteUsersToWorkspaceCommand([]),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldSendEmail_ForEachNewInvite()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        SetupValidWorkspace(workspaceKey, workspace, [], []);

        await Handler.Handle(
            new InviteUsersToWorkspaceCommand(["user1@email.com", "user2@email.com"]),
            TestContext.Current.CancellationToken);

        await Email.Received(2).Send(Arg.Any<SendEmailModel>());
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldNotSendEmail_ForExistingWorkspaceMembers()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var existingMember = new AppUser { Id = "userId", Email = "member@email.com" };

        SetupValidWorkspace(workspaceKey, workspace, [existingMember], [existingMember]);

        var result = await Handler.Handle(
            new InviteUsersToWorkspaceCommand(["member@email.com", "newuser@email.com"]),
            TestContext.Current.CancellationToken);

        result.Payload!.Emails.Should().ContainSingle("newuser@email.com");
        await Email.Received(1).Send(Arg.Any<SendEmailModel>());
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldRefreshExistingPendingInvite_NotCreateDuplicate()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var existingInvite = new WorkspaceInvite { Email = "pending@email.com", WorkspaceId = workspace.Id, Code = "oldcode" };

        SetupValidWorkspace(workspaceKey, workspace, [], [], [existingInvite]);

        await Handler.Handle(
            new InviteUsersToWorkspaceCommand(["pending@email.com"]),
            TestContext.Current.CancellationToken);

        await UnitOfWork.WorkspaceInvites.DidNotReceive().AddAsync(Arg.Any<WorkspaceInvite>(), Arg.Any<CancellationToken>());
        existingInvite.Code.Should().NotBe("oldcode");
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldCallAddRangeAsync_ForNewInvitees()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        SetupValidWorkspace(workspaceKey, workspace, [], []);

        await Handler.Handle(
            new InviteUsersToWorkspaceCommand(["new1@email.com", "new2@email.com"]),
            TestContext.Current.CancellationToken);

        await UnitOfWork.WorkspaceInvites.Received(1)
            .AddRangeAsync(Arg.Is<IEnumerable<WorkspaceInvite>>(list => list.Count() == 2), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InviteUsersToWorkspace_ShouldCallCompleteAsync()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        SetupValidWorkspace(workspaceKey, workspace, [], []);

        await Handler.Handle(
            new InviteUsersToWorkspaceCommand(["user@email.com"]),
            TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }
}
