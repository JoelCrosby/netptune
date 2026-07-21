using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Models;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Users.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Users.Commands;

public class UpdateWorkspaceRoleCommandHandlerTests
{
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly IEventRecordWriter EventRecords = Substitute.For<IEventRecordWriter>();
    private readonly UpdateWorkspaceRoleCommandHandler Handler;

    public UpdateWorkspaceRoleCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, PermissionCache, EventRecords);
    }

    [Fact]
    public async Task UpdateRole_ShouldReplacePermissionsWithRoleDefaults()
    {
        const string workspaceKey = "workspace";
        const string targetUserId = "target";
        var workspace = AutoFixtures.Workspace;

        Identity.GetCurrentUserId().Returns("actor");
        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Workspaces.GetBySlug(workspaceKey, true, TestContext.Current.CancellationToken)
            .Returns(workspace);
        UnitOfWork.WorkspaceUsers.GetUserPermissions(
                targetUserId,
                workspaceKey,
                false,
                TestContext.Current.CancellationToken)
            .Returns(new UserPermissions
            {
                UserId = targetUserId,
                WorkspaceKey = workspaceKey,
                Role = WorkspaceRole.Member,
                Permissions = [NetptunePermissions.Tasks.Update],
            });

        var result = await Handler.Handle(
            new UpdateWorkspaceRoleCommand(new UpdateWorkspaceRoleRequest
            {
                UserId = targetUserId,
                Role = WorkspaceRole.Viewer,
            }),
            TestContext.Current.CancellationToken);

        var defaults = WorkspaceRolePermissions.GetDefaultPermissions(WorkspaceRole.Viewer);

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Role.Should().Be(WorkspaceRole.Viewer);
        result.Payload.Permissions.Should().BeEquivalentTo(defaults);

        await UnitOfWork.WorkspaceUsers.Received(1).SetUserRole(
            targetUserId,
            workspace.Id,
            WorkspaceRole.Viewer,
            Arg.Is<IEnumerable<string>>(permissions => permissions.ToHashSet().SetEquals(defaults)),
            TestContext.Current.CancellationToken);

        PermissionCache.Received(1).Remove(Arg.Is<WorkspaceUserKey>(key =>
            key.UserId == targetUserId && key.WorkspaceKey == workspaceKey));

        await EventRecords.Received(1).Append(
            Arg.Is<EventWriteRequest<WorkspaceRoleChangedPayload>>(eventRequest =>
                eventRequest.EventKey == EventKeys.WorkspaceRoleChanged &&
                eventRequest.WorkspaceId == workspace.Id &&
                eventRequest.SubjectType == EventEntityTypes.From(EntityType.Workspace) &&
                eventRequest.SubjectId == workspace.Id.ToString() &&
                eventRequest.Payload.TargetUserId == targetUserId &&
                eventRequest.Payload.OldRole == WorkspaceRole.Member.ToString() &&
                eventRequest.Payload.NewRole == WorkspaceRole.Viewer.ToString()),
            TestContext.Current.CancellationToken);
    }

    [Theory]
    [InlineData(WorkspaceRole.Owner)]
    [InlineData((WorkspaceRole)42)]
    public async Task UpdateRole_ShouldRejectUnsupportedRoles(WorkspaceRole role)
    {
        Identity.GetCurrentUserId().Returns("actor");

        var result = await Handler.Handle(
            new UpdateWorkspaceRoleCommand(new UpdateWorkspaceRoleRequest
            {
                UserId = "target",
                Role = role,
            }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        await UnitOfWork.WorkspaceUsers.DidNotReceiveWithAnyArgs()
            .SetUserRole(default!, default, default, default!, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task UpdateRole_ShouldRejectChangingYourOwnRole()
    {
        Identity.GetCurrentUserId().Returns("same-user");

        var result = await Handler.Handle(
            new UpdateWorkspaceRoleCommand(new UpdateWorkspaceRoleRequest
            {
                UserId = "same-user",
                Role = WorkspaceRole.Viewer,
            }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
    }

}
