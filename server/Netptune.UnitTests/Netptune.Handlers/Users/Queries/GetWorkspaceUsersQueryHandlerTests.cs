using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Users.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Queries;

public class GetWorkspaceUsersQueryHandlerTests
{
    private readonly GetWorkspaceUsersQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetWorkspaceUsersQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    private void SetupWorkspace(string workspaceKey, Workspace? workspace,
        List<WorkspaceAppUser> members, List<WorkspaceInvite> pendingInvites)
    {
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>(), Arg.Any<PageRequest?>()).Returns(members);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace!);
        if (workspace is not null)
            UnitOfWork.WorkspaceInvites.GetPendingByWorkspace(workspace.Id, Arg.Any<CancellationToken>()).Returns(pendingInvites);
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnMembers()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var member = AutoFixtures.WorkspaceAppUser;

        SetupWorkspace(workspaceKey, workspace, [member], []);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result[0].IsPending.Should().BeFalse();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldIncludePendingInvites()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var pendingInvite = new WorkspaceInvite { Email = "pending@email.com", WorkspaceId = workspace.Id, Code = "code" };

        SetupWorkspace(workspaceKey, workspace, [], [pendingInvite]);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result[0].Email.Should().Be("pending@email.com");
        result[0].IsPending.Should().BeTrue();
        result[0].Role.Should().Be(WorkspaceRole.Member);
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnBothMembersAndPendingInvites()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var member = AutoFixtures.WorkspaceAppUser;
        var pendingInvite = new WorkspaceInvite { Email = "pending@email.com", WorkspaceId = workspace.Id, Code = "code" };

        SetupWorkspace(workspaceKey, workspace, [member], [pendingInvite]);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().HaveCount(2);
        result.Where(u => u.IsPending).Should().ContainSingle();
        result.Where(u => !u.IsPending).Should().ContainSingle();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldNotDuplicatePendingInvite_WhenUserAlreadyMember()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var member = AutoFixtures.WorkspaceAppUser;
        var duplicateInvite = new WorkspaceInvite { Email = member.User.Email!, WorkspaceId = workspace.Id, Code = "code" };

        SetupWorkspace(workspaceKey, workspace, [member], [duplicateInvite]);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().ContainSingle();
        result[0].IsPending.Should().BeFalse();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnEmpty_WhenNoMembersAndNoInvites()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        SetupWorkspace(workspaceKey, workspace, [], []);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnEmpty_WhenWorkspaceNotFound()
    {
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>(), Arg.Any<PageRequest?>()).Returns([]);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).ReturnsNull();

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.Should().BeEmpty();
    }
}
