using FluentAssertions;

using Netptune.Core.Authorization;
using Netptune.Core.Cache;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models;
using Netptune.Core.Repositories;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Pins.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pins.Commands;

public class DeleteTaskPinCommandHandlerTests
{
    private const int WorkspaceId = 123;
    private const int PinId = 4;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly ITaskPinRepository TaskPins = Substitute.For<ITaskPinRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly DeleteTaskPinCommandHandler Handler;

    public DeleteTaskPinCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.TryGetCurrentUserId().Returns("user-1");
        Identity.TryGetWorkspaceKey().Returns("workspace");

        GrantRole(WorkspaceRole.Member);

        Handler = new DeleteTaskPinCommandHandler(UnitOfWork, TaskPins, Identity, PermissionCache);
    }

    [Fact]
    public async Task Handle_ShouldNotFound_WhenThePinDoesNotExist()
    {
        var result = await Send();

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRemoveYourOwnPersonalPin()
    {
        var pin = Existing(TaskPinScope.User, "user-1");

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        pin.IsDeleted.Should().BeTrue();
        pin.DeletedByUserId.Should().Be("user-1");

        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldNotFound_WhenThePinBelongsToSomebodyElse()
    {
        var pin = Existing(TaskPinScope.User, "user-2");

        var result = await Send();

        result.IsNotFound.Should().BeTrue();
        pin.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRefuseASharedPin_WhenTheCallerCannotWriteAtThatScope()
    {
        GrantRole(WorkspaceRole.Viewer);

        var pin = Existing(TaskPinScope.Board, "user-2");

        var result = await Send();

        result.IsForbidden.Should().BeTrue();
        pin.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldRemoveASharedPin_WhenTheCallerCanWriteAtThatScope()
    {
        var pin = Existing(TaskPinScope.Board, "user-2");

        var result = await Send();

        result.IsSuccess.Should().BeTrue();
        pin.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuseAnAnonymousCaller_ReadingAPublicWorkspace()
    {
        Identity.TryGetCurrentUserId().ReturnsNull();

        var pin = Existing(TaskPinScope.Workspace, "user-2");

        var result = await Send();

        result.IsForbidden.Should().BeTrue();
        pin.IsDeleted.Should().BeFalse();

        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    private Task<ClientResponse> Send()
    {
        return Handler.Handle(new DeleteTaskPinCommand(PinId), TestContext.Current.CancellationToken).AsTask();
    }

    private TaskPin Existing(TaskPinScope scope, string createdByUserId)
    {
        var pin = new TaskPin
        {
            Id = PinId,
            ProjectTaskId = 9,
            Scope = scope,
            ScopeEntityId = 77,
            WorkspaceId = WorkspaceId,
            CreatedByUserId = createdByUserId,
        };

        TaskPins.GetInWorkspace(PinId, WorkspaceId, false, Arg.Any<CancellationToken>()).Returns(pin);

        return pin;
    }

    private void GrantRole(WorkspaceRole role)
    {
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = role,
            Permissions = role == WorkspaceRole.Viewer ? [] : [NetptunePermissions.Boards.Update],
        });
    }
}
