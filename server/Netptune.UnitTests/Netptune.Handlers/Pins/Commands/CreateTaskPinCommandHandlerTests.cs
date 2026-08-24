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
using Netptune.Core.ViewModels.Pins;
using Netptune.Handlers.Pins.Commands;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Pins.Commands;

public class CreateTaskPinCommandHandlerTests
{
    private const int WorkspaceId = 123;
    private const int ProjectId = 44;
    private const int BoardId = 77;
    private const int TaskId = 9;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly ITaskPinRepository TaskPins = Substitute.For<ITaskPinRepository>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IWorkspacePermissionCache PermissionCache = Substitute.For<IWorkspacePermissionCache>();
    private readonly CreateTaskPinCommandHandler Handler;

    public CreateTaskPinCommandHandlerTests()
    {
        Identity.GetWorkspaceId().Returns(WorkspaceId);
        Identity.GetCurrentUserId().Returns("user-1");
        Identity.TryGetWorkspaceKey().Returns("workspace");

        GrantRole(WorkspaceRole.Member);

        UnitOfWork.Tasks
            .GetInWorkspace(TaskId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new ProjectTask { Id = TaskId, WorkspaceId = WorkspaceId, ProjectId = ProjectId });

        UnitOfWork.Boards
            .GetInWorkspace(BoardId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Board { Id = BoardId, Name = "Sprint board", WorkspaceId = WorkspaceId, ProjectId = ProjectId });

        UnitOfWork.Projects
            .GetInWorkspace(ProjectId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Project { Id = ProjectId, Name = "Netptune Web", WorkspaceId = WorkspaceId });

        UnitOfWork.Boards.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), true, Arg.Any<CancellationToken>()).Returns([]);
        UnitOfWork.Projects.GetAllByIdAsync(Arg.Any<IEnumerable<int>>(), true, Arg.Any<CancellationToken>()).Returns([]);
        UnitOfWork.Workspaces.GetAsync(WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new Workspace { Id = WorkspaceId, Name = "Netptune", Slug = "workspace" });

        TaskPins.AddAsync(Arg.Any<TaskPin>(), Arg.Any<CancellationToken>()).Returns(call => call.Arg<TaskPin>());

        Handler = new CreateTaskPinCommandHandler(UnitOfWork, TaskPins, Identity, PermissionCache);
    }

    [Fact]
    public async Task Handle_ShouldResolveTheWorkspace_ForAPersonalPin()
    {
        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.User });

        result.IsSuccess.Should().BeTrue();
        result.Payload!.ScopeEntityId.Should().Be(WorkspaceId);
        result.Payload.Scope.Should().Be(TaskPinScope.User);
        result.Payload.CanUnpin.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldResolveTheWorkspace_ForAWorkspacePin()
    {
        GrantRole(WorkspaceRole.Admin);

        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.Workspace });

        result.IsSuccess.Should().BeTrue();
        result.Payload!.ScopeEntityId.Should().Be(WorkspaceId);
        result.Payload.ScopeName.Should().Be("Netptune");
    }

    [Fact]
    public async Task Handle_ShouldFallBackToTheTasksProject_WhenAProjectIsNotGiven()
    {
        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.Project });

        result.IsSuccess.Should().BeTrue();
        result.Payload!.ScopeEntityId.Should().Be(ProjectId);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenABoardPinNamesNoBoard()
    {
        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.Board });

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("A board is required to pin to a board.");
    }

    [Fact]
    public async Task Handle_ShouldNotFound_WhenTheTaskIsSoftDeleted()
    {
        UnitOfWork.Tasks
            .GetInWorkspace(TaskId, WorkspaceId, true, Arg.Any<CancellationToken>())
            .Returns(new ProjectTask { Id = TaskId, WorkspaceId = WorkspaceId, IsDeleted = true });

        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.User });

        result.IsNotFound.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuseAWorkspacePin_WhenTheCallerLacksThePermission()
    {
        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.Workspace });

        result.IsForbidden.Should().BeTrue();

        await TaskPins.DidNotReceive().AddAsync(Arg.Any<TaskPin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldAllowAWorkspacePin_WhenTheCallerHasThePermission()
    {
        GrantPermission(NetptunePermissions.Tasks.PinWorkspace);

        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.Workspace });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldRefuseABoardPin_WhenTheCallerIsAViewer()
    {
        GrantRole(WorkspaceRole.Viewer);

        var request = new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.Board, ScopeEntityId = BoardId };
        var result = await Send(request);

        result.IsForbidden.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldAllowAPersonalPin_WhenTheCallerIsAViewer()
    {
        GrantRole(WorkspaceRole.Viewer);

        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.User });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnTheExistingPin_WhenTheTaskIsAlreadyPinned()
    {
        var existing = new TaskPin
        {
            Id = 5,
            ProjectTaskId = TaskId,
            Scope = TaskPinScope.User,
            ScopeEntityId = WorkspaceId,
            WorkspaceId = WorkspaceId,
            CreatedByUserId = "user-1",
        };

        TaskPins.Find(TaskId, TaskPinScope.User, WorkspaceId, "user-1", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.User });

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Id.Should().Be(5);

        await TaskPins.DidNotReceive().AddAsync(Arg.Any<TaskPin>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ShouldReviveATombstone_RatherThanInsertADuplicate()
    {
        var tombstone = new TaskPin
        {
            Id = 5,
            ProjectTaskId = TaskId,
            Scope = TaskPinScope.User,
            ScopeEntityId = WorkspaceId,
            WorkspaceId = WorkspaceId,
            CreatedByUserId = "user-1",
            DeletedByUserId = "user-1",
            IsDeleted = true,
        };

        TaskPins.Find(TaskId, TaskPinScope.User, WorkspaceId, "user-1", Arg.Any<CancellationToken>()).Returns(tombstone);
        TaskPins.GetNextSortOrder(WorkspaceId, TaskPinScope.User, WorkspaceId, Arg.Any<CancellationToken>()).Returns(-3d);

        var result = await Send(new CreateTaskPinRequest { TaskId = TaskId, Scope = TaskPinScope.User });

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Id.Should().Be(5);
        result.Payload.SortOrder.Should().Be(-3d);

        tombstone.IsDeleted.Should().BeFalse();
        tombstone.DeletedByUserId.Should().BeNull();

        await TaskPins.DidNotReceive().AddAsync(Arg.Any<TaskPin>(), Arg.Any<CancellationToken>());
        await UnitOfWork.Received(1).CompleteAsync(Arg.Any<CancellationToken>());
    }

    private Task<ClientResponse<TaskPinViewModel>> Send(CreateTaskPinRequest request)
    {
        return Handler.Handle(new CreateTaskPinCommand(request), TestContext.Current.CancellationToken).AsTask();
    }

    private void GrantRole(WorkspaceRole role)
    {
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = role,
            Permissions = role == WorkspaceRole.Viewer ? [] : [NetptunePermissions.Boards.Update, NetptunePermissions.Projects.Update],
        });
    }

    private void GrantPermission(string permission)
    {
        PermissionCache.GetUserPermissions("user-1", "workspace").Returns(new UserPermissions
        {
            UserId = "user-1",
            WorkspaceKey = "workspace",
            Role = WorkspaceRole.Member,
            Permissions = [permission],
        });
    }
}
