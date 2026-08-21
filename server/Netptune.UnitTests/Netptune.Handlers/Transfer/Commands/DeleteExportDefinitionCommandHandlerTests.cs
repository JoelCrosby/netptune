using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Models;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Transfer.Commands;
using Netptune.Transfer.Entities;
using Netptune.Transfer.Enums;
using Netptune.Transfer.Repositories;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Transfer.Commands;

public class DeleteExportDefinitionCommandHandlerTests
{
    private const int WorkspaceId = 2;
    private const string WorkspaceKey = "workspace";
    private const string UserId = "user-id";

    private readonly DeleteExportDefinitionCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IExportDefinitionRepository ExportDefinitions = Substitute.For<IExportDefinitionRepository>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();

    public DeleteExportDefinitionCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, ExportDefinitions, PermissionCache);
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns(UserId);
        Identity.TryGetWorkspaceKey().Returns(WorkspaceKey);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheMembersOwnPrivateDefinition()
    {
        var definition = Given(NewDefinition(UserId, isShared: false));
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Member));

        var result = await Handler.Handle(new DeleteExportDefinitionCommand(definition.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        definition.IsDeleted.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenDeletingASharedDefinitionWithoutManageDefinitions()
    {
        var definition = Given(NewDefinition(UserId, isShared: true));
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Member));

        var result = await Handler.Handle(new DeleteExportDefinitionCommand(definition.Id), TestContext.Current.CancellationToken);

        result.IsForbidden.Should().BeTrue();
        definition.IsDeleted.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldReturnForbidden_WhenDeletingAnotherMembersDefinition()
    {
        var definition = Given(NewDefinition("another-user", isShared: false));
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Member));

        var result = await Handler.Handle(new DeleteExportDefinitionCommand(definition.Id), TestContext.Current.CancellationToken);

        result.IsForbidden.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_ShouldAllowAnAdminToRemoveASharedDefinition()
    {
        var definition = Given(NewDefinition("another-user", isShared: true));
        PermissionCache.GetUserPermissions(UserId, WorkspaceKey).Returns(Permissions(WorkspaceRole.Admin));

        var result = await Handler.Handle(new DeleteExportDefinitionCommand(definition.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        definition.IsDeleted.Should().BeTrue();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenTheDefinitionIsNotInTheWorkspace()
    {
        ExportDefinitions.GetInWorkspace(42, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new DeleteExportDefinitionCommand(42), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    private ExportDefinition Given(ExportDefinition definition)
    {
        ExportDefinitions.GetInWorkspace(definition.Id, WorkspaceId, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(definition);

        return definition;
    }

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
