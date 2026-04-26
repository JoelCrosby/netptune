using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Responses.Common;
using Netptune.Core.Models.Activity;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services.Tasks.Commands;
using Netptune.Services.Tasks.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class CreateTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly CreateTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly ILogger<CreateTaskCommandHandler> Logger = Substitute.For<ILogger<CreateTaskCommandHandler>>();

    public CreateTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Identity, Activity, Logger);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel
        {
            Name = request.Name,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>(), Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Create_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace);

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsNull();

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenScopeRefIdNull()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).ReturnsNull();
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldLogActivity_WhenInputValid()
    {
        var request = Fixture.Build<AddProjectTaskRequest>().With(p => p.ProjectId, 1).Create();
        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);
        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());
        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>(), Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        await Handler.Handle(new CreateTaskCommand(request), CancellationToken.None);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }
}

public class DeleteTaskCommandHandlerTests
{
    private readonly DeleteTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(1).Returns(AutoFixtures.ProjectTask);

        var result = await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallDeletePermanent_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(taskToDelete);

        await Handler.Handle(new DeleteTaskCommand(taskToDelete.Id), CancellationToken.None);

        await UnitOfWork.Tasks.Received(1).DeletePermanent(taskToDelete.Id);
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(taskToDelete);

        await Handler.Handle(new DeleteTaskCommand(taskToDelete.Id), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        var result = await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        await UnitOfWork.Received(0).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldLogActivity_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(AutoFixtures.ProjectTask);

        await Handler.Handle(new DeleteTaskCommand(1), CancellationToken.None);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }
}

public class DeleteTasksCommandHandlerTests
{
    private readonly DeleteTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public DeleteTasksCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task DeleteMany_ShouldReturnSuccess_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>()).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        var result = await Handler.Handle(new DeleteTasksCommand(ids), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task DeleteMany_ShouldCallDeletePermanent_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>()).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        await Handler.Handle(new DeleteTasksCommand(ids), CancellationToken.None);

        await UnitOfWork.Tasks.Received(1).DeletePermanent(Arg.Any<IEnumerable<int>>());
    }

    [Fact]
    public async Task DeleteMany_ShouldCallCompleteAsync_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>()).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        await Handler.Handle(new DeleteTasksCommand(ids), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task DeleteMany_ShouldLogActivity_WhenValidId()
    {
        var ids = new[] { 1, 2, 3 };
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<int[]>()).Returns(new List<ProjectTask>
        {
            new () { Id = 1 },
            new () { Id = 2 },
            new () { Id = 3 },
        });

        await Handler.Handle(new DeleteTasksCommand(ids), CancellationToken.None);

        Activity.Received(1).LogMany(Arg.Any<Action<ActivityMultipleOptions>>());
    }
}

public class GetTaskQueryHandlerTests
{
    private readonly GetTaskQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public GetTaskQueryHandlerTests()
    {
        Handler = new(UnitOfWork);
    }

    [Fact]
    public async Task GetTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var task = AutoFixtures.TaskViewModel;
        UnitOfWork.Tasks.GetTaskViewModel(1).Returns(task);

        var result = await Handler.Handle(new GetTaskQuery(1), CancellationToken.None);

        result.Should().BeEquivalentTo(task);
    }
}

public class GetTaskDetailQueryHandlerTests
{
    private readonly GetTaskDetailQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTaskDetailQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetTaskDetail_ShouldReturnCorrectly_WhenInputValid()
    {
        var task = AutoFixtures.TaskViewModel;
        const string systemId = "systemId";
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Tasks.GetTaskViewModel(systemId, workspaceKey).Returns(task);

        var result = await Handler.Handle(new GetTaskDetailQuery(systemId), CancellationToken.None);

        result.Should().BeEquivalentTo(task);
    }
}

public class GetTasksQueryHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly GetTasksQueryHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public GetTasksQueryHandlerTests()
    {
        Handler = new(UnitOfWork, Identity);
    }

    [Fact]
    public async Task GetTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var tasks = Fixture.Create<List<TaskViewModel>>();
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Tasks.GetTasksAsync(workspaceKey).Returns(tasks);

        var result = await Handler.Handle(new GetTasksQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(tasks);
    }
}

public class UpdateTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly ILogger<UpdateTaskCommandHandler> Logger = Substitute.For<ILogger<UpdateTaskCommandHandler>>();

    public UpdateTaskCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity, Logger);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = AutoFixtures.ProjectTask;
        var viewModel = new TaskViewModel
        {
            Name = request.Name!,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };

        UnitOfWork.Tasks.GetAsync(request.Id).Returns(task);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetBoardGroupsForProjectTask(Arg.Any<int>()).Returns(AutoFixtures.BoardGroups);
        UnitOfWork.Boards.GetDefaultBoardInProject(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(AutoFixtures.Board);
        UnitOfWork.InvokeTransaction();

        var result = await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
    }

    [Fact]
    public async Task Update_ShouldReturnFailed_WhenIdNotFound()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        UnitOfWork.Tasks.GetAsync(request.Id).ReturnsNull();

        var result = await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldCallTransaction_WhenInputValid()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = AutoFixtures.ProjectTask;
        var viewModel = new TaskViewModel
        {
            Name = request.Name!,
            Description = request.Description,
            SortOrder = request.SortOrder ?? 8,
        };

        UnitOfWork.Tasks.GetAsync(request.Id).Returns(task);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetBoardGroupsForProjectTask(Arg.Any<int>()).Returns(AutoFixtures.BoardGroups);
        UnitOfWork.Boards.GetDefaultBoardInProject(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(AutoFixtures.Board);
        UnitOfWork.InvokeTransaction();

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>());
    }
}

public class MoveTaskInBoardGroupCommandHandlerTests
{
    private readonly MoveTaskInBoardGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public MoveTaskInBoardGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 2,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>()).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId).Returns(AutoFixtures.ProjectTask);

        var result = await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 2,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>()).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId).Returns(AutoFixtures.ProjectTask);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

        await UnitOfWork.Received(2).CompleteAsync();
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_ShouldLogActivity_WhenValidId()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 2,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>()).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId).Returns(AutoFixtures.ProjectTask);

        await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

        Activity.Received(1).LogWith(Arg.Any<Action<ActivityOptions<MoveTaskActivityMeta>>>());
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_SameGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = new MoveTaskInGroupRequest
        {
            CurrentIndex = 1,
            PreviousIndex = 0,
            TaskId = 1,
            NewGroupId = 1,
            OldGroupId = 1,
        };

        var taskInGroupA = AutoFixtures.ProjectTaskInBoardGroup with { ProjectTaskId = request.TaskId };
        var taskInGroupB = AutoFixtures.ProjectTaskInBoardGroup;
        var taskInGroups = new List<ProjectTaskInBoardGroup> { taskInGroupA, taskInGroupB };

        UnitOfWork.InvokeTransaction<BoardGroup>();
        UnitOfWork.ProjectTasksInGroups.GetProjectTaskInGroup(request.TaskId, request.NewGroupId).Returns(taskInGroupA);
        UnitOfWork.ProjectTasksInGroups.GetProjectTasksInGroup(Arg.Any<int>()).Returns(taskInGroups);
        UnitOfWork.BoardGroups.GetTasksInGroup(request.NewGroupId).Returns(AutoFixtures.ProjectTasks);
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetAsync(request.TaskId).Returns(AutoFixtures.ProjectTask);

        var result = await Handler.Handle(new MoveTaskInBoardGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }
}

public class MoveTasksToGroupCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly MoveTasksToGroupCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public MoveTasksToGroupCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());

        var result = await Handler.Handle(new MoveTasksToGroupCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());

        await Handler.Handle(new MoveTasksToGroupCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture.Build<MoveTasksToGroupRequest>().Create();
        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value).Returns(AutoFixtures.BoardGroup);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());

        await Handler.Handle(new MoveTasksToGroupCommand(request), CancellationToken.None);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<MoveTaskActivityMeta>>>());
    }
}

public class ReassignTasksCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly ReassignTasksCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();

    public ReassignTasksCommandHandlerTests()
    {
        Handler = new(UnitOfWork, Activity);
    }

    [Fact]
    public async Task ReassignTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        var result = await Handler.Handle(new ReassignTasksCommand(request), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReassignTasks_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        await Handler.Handle(new ReassignTasksCommand(request), CancellationToken.None);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task ReassignTasks_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture.Build<ReassignTasksRequest>().Create();
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        await Handler.Handle(new ReassignTasksCommand(request), CancellationToken.None);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<AssignActivityMeta>>>());
    }
}
