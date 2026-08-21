using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Transfer.Commands;
using Netptune.Transfer.Definitions;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Transfer.Commands;

public class SaveExportDefinitionCommandHandlerTests
{
    private const int WorkspaceId = 2;
    private const string WorkspaceKey = "workspace";
    private const string UserId = "user-id";

    private readonly SaveExportDefinitionCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IExportDefinitionRepository ExportDefinitions = Substitute.For<IExportDefinitionRepository>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();

    public SaveExportDefinitionCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, ExportDefinitions, PermissionCache);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
    }

    [Fact]
    public async Task Save_ShouldStoreAPrivateDefinition_WhenTheMemberCannotManageDefinitions()
    {
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Member));

        var result = await Handler.Handle(Command(isShared: false), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await ExportDefinitions.Received(1).AddAsync(
            Arg.Is<ExportDefinition>(definition => definition.OwnerId == UserId && !definition.IsShared),
            TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Save_ShouldReturnForbidden_WhenSharingWithoutManageDefinitions()
    {
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Member));

        var result = await Handler.Handle(Command(isShared: true), TestContext.Current.CancellationToken);

        result.IsForbidden.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Save_ShouldShare_WhenTheMemberHasManageDefinitions()
    {
        var permissions = Permissions(WorkspaceRole.Member, NetptunePermissions.Data.ManageDefinitions);
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(permissions);

        var result = await Handler.Handle(Command(isShared: true), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Save_ShouldReturnForbidden_WhenEditingAnotherMembersDefinition()
    {
        var existing = NewDefinition("another-user", isShared: false);
        ExportDefinitions.GetInWorkspace(existing.Id, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(existing);
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Member));

        var command = Command(isShared: false, id: existing.Id);
        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsForbidden.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Save_ShouldAllowAnAdminToEditASharedDefinition()
    {
        var existing = NewDefinition("another-user", isShared: true);
        ExportDefinitions.GetInWorkspace(existing.Id, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(existing);
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Admin));

        var command = Command(isShared: true, id: existing.Id);
        var result = await Handler.Handle(command, TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    private static SaveExportDefinitionCommand Command(bool isShared, int? id = null) => new(new SaveExportDefinitionRequest
    {
        Id = id,
        Name = "Weekly tasks",
        IsShared = isShared,
        Definition = new ExportDefinitionModel
        {
            RecordType = "task",
            Format = ExportFormat.Csv,
            Fields = ["task.name"],
        },
    });

    private static ExportDefinition NewDefinition(string ownerId, bool isShared) => new()
    {
        Id = 7,
        Name = "Weekly tasks",
        RecordType = "task",
        Format = ExportFormat.Csv,
        IsShared = isShared,
        OwnerId = ownerId,
        WorkspaceId = WorkspaceId,
    };

    private static UserPermissions Permissions(WorkspaceRole role, params string[] permissions) => new()
    {
        UserId = UserId,
        WorkspaceKey = WorkspaceKey,
        Role = role,
        Permissions = [.. permissions],
    };
}
