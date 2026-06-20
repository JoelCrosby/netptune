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
        var memberEmails = members
            .Select(member => member.User.Email)
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var pendingInvitesExcludingMembers = pendingInvites
            .Where(invite => !memberEmails.Contains(invite.Email))
            .ToList();

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Users.GetWorkspaceAppUsers(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>(), Arg.Any<PageRequest?>())
            .Returns(call =>
            {
                var pageRequest = call.ArgAt<PageRequest?>(3);

                if (pageRequest is null)
                {
                    return members;
                }

                var skip = (pageRequest.GetPage() - 1) * pageRequest.GetPageSize();

                return members
                    .Skip(skip)
                    .Take(pageRequest.GetPageSize())
                    .ToList();
            });
        UnitOfWork.Users.CountWorkspaceAppUsers(workspaceKey, Arg.Any<CancellationToken>()).Returns(members.Count);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(workspace!);
        if (workspace is not null)
        {
            UnitOfWork.WorkspaceInvites.CountPendingByWorkspaceExcludingMembers(workspace.Id, Arg.Any<CancellationToken>())
                .Returns(pendingInvitesExcludingMembers.Count);
            UnitOfWork.WorkspaceInvites.GetPendingByWorkspaceExcludingMembers(
                    workspace.Id,
                    Arg.Any<int>(),
                    Arg.Any<int>(),
                    Arg.Any<CancellationToken>())
                .Returns(call =>
                {
                    var skip = call.ArgAt<int>(1);
                    var take = call.ArgAt<int>(2);

                    return pendingInvitesExcludingMembers.Skip(skip).Take(take).ToList();
                });
        }
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnMembers()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var member = AutoFixtures.WorkspaceAppUser;

        SetupWorkspace(workspaceKey, workspace, [member], []);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle();
        result.Payload.Items[0].IsPending.Should().BeFalse();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldIncludePendingInvites()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var pendingInvite = new WorkspaceInvite { Email = "pending@email.com", WorkspaceId = workspace.Id, Code = "code" };

        SetupWorkspace(workspaceKey, workspace, [], [pendingInvite]);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle();
        result.Payload.Items[0].Email.Should().Be("pending@email.com");
        result.Payload.Items[0].IsPending.Should().BeTrue();
        result.Payload.Items[0].Role.Should().Be(WorkspaceRole.Member);
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

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().HaveCount(2);
        result.Payload.Items.Where(u => u.IsPending).Should().ContainSingle();
        result.Payload.Items.Where(u => !u.IsPending).Should().ContainSingle();
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

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle();
        result.Payload.Items[0].IsPending.Should().BeFalse();
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnEmpty_WhenNoMembersAndNoInvites()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;

        SetupWorkspace(workspaceKey, workspace, [], []);

        var result = await Handler.Handle(new GetWorkspaceUsersQuery(), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().BeEmpty();
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
    }

    [Fact]
    public async Task GetWorkspaceUsers_ShouldReturnRequestedPage()
    {
        const string workspaceKey = "workspaceKey";
        var workspace = AutoFixtures.Workspace;
        var members = Enumerable
            .Range(1, 3)
            .Select(i =>
            {
                var user = AutoFixtures.AppUser;
                user.Id = $"user-{i}";
                user.Email = $"user-{i}@email.com";
                user.Firstname = $"User {i}";

                return AutoFixtures.WorkspaceAppUser with
                {
                    UserId = $"user-{i}",
                    User = user,
                };
            })
            .ToList();

        SetupWorkspace(workspaceKey, workspace, members, []);

        var result = await Handler.Handle(
            new GetWorkspaceUsersQuery(new PageRequest { Page = 2, PageSize = 2 }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Items.Should().ContainSingle();
        result.Payload.Page.Should().Be(2);
        result.Payload.PageSize.Should().Be(2);
        result.Payload.TotalCount.Should().Be(3);
        result.Payload.TotalPages.Should().Be(2);
    }
}
