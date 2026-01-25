using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Relationships;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services;

public class TaskServiceTests
{
    private readonly Fixture Fixture = new();

    private readonly TaskService Service;

    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly ILogger<TaskService> Logger = Substitute.For<ILogger<TaskService>>();

    public TaskServiceTests()
    {
        Service = new(UnitOfWork, Identity, Activity, Logger);
    }

    [Fact]
    public async Task Create_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

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

        var result = await Service.Create(request);

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
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description, };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        await Service.Create(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenProjectNotFound()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace);

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenScopeIdNotFound()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace);

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenWorkspaceNotFound()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsNull();

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldReturnFailure_WhenScopeRefIdNull()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description, };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>()).ReturnsNull();
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        var result = await Service.Create(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Create_ShouldLogActivity_WhenInputValid()
    {
        var request = Fixture
            .Build<AddProjectTaskRequest>()
            .With(p => p.ProjectId, 1)
            .Create();

        var viewModel = new TaskViewModel { Name = request.Name, Description = request.Description, };

        Identity.GetWorkspaceKey().Returns("key");
        Identity.GetCurrentUser().Returns(AutoFixtures.AppUser);

        UnitOfWork.Workspaces
            .GetBySlugWithTasks(Arg.Any<string>(), Arg.Any<bool>())
            .ReturnsForAnyArgs(AutoFixtures.Workspace.WithProjects());

        UnitOfWork.Tasks.AddAsync(Arg.Any<ProjectTask>()).Returns(AutoFixtures.ProjectTask);
        UnitOfWork.Tasks.GetNextScopeId(Arg.Any<int>(), Arg.Any<int>()).Returns(Fixture.Create<int>());
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetWithTasksInGroups(Arg.Any<int>()).Returns(AutoFixtures.BoardGroup.WithTasks());

        await Service.Create(request);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(1).Returns(AutoFixtures.ProjectTask);

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_ShouldCallDeletePermanent_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;

        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(taskToDelete);

        await Service.Delete(taskToDelete.Id);

        await UnitOfWork.Tasks.Received(1).DeletePermanent(taskToDelete.Id);
    }

    [Fact]
    public async Task Delete_ShouldCallCompleteAsync_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;

        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(taskToDelete);

        await Service.Delete(taskToDelete.Id);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldReturnFailure_WhenInvalidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        var result = await Service.Delete(1);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Delete_ShouldNotCallDeletePermanent_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Tasks.Received(0).DeletePermanent(Arg.Any<int>());
    }

    [Fact]
    public async Task Delete_ShouldNotCallCompleteAsync_WhenValidId()
    {
        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).ReturnsNull();

        await Service.Delete(1);

        await UnitOfWork.Received(0).CompleteAsync();
    }

    [Fact]
    public async Task Delete_ShouldLogActivity_WhenValidId()
    {
        var taskToDelete = AutoFixtures.ProjectTask;

        UnitOfWork.Tasks.GetAsync(Arg.Any<int>()).Returns(taskToDelete);

        await Service.Delete(1);

        Activity.Received(1).Log(Arg.Any<Action<ActivityOptions>>());
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

        var result = await Service.Delete(ids);

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

        await Service.Delete(ids);

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

        await Service.Delete(ids);

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

        await Service.Delete(ids);

        Activity.Received(1).LogMany(Arg.Any<Action<ActivityMultipleOptions>>());
    }

    [Fact]
    public async Task GetTask_ShouldReturnCorrectly_WhenInputValid()
    {
        var task = AutoFixtures.TaskViewModel;

        UnitOfWork.Tasks.GetTaskViewModel(1).Returns(task);

        var result = await Service.GetTask(1);

        result.Should().BeEquivalentTo(task);
    }

    [Fact]
    public async Task GetTaskDetail_ShouldReturnCorrectly_WhenInputValid()
    {
        var task = AutoFixtures.TaskViewModel;
        const string systemId = "systemId";
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Tasks.GetTaskViewModel(systemId, workspaceKey).Returns(task);

        var result = await Service.GetTaskDetail(systemId);

        result.Should().BeEquivalentTo(task);
    }

    [Fact]
    public async Task GetTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var tasks = Fixture.Create<List<TaskViewModel>>();
        const string workspaceKey = "workspaceKey";

        Identity.GetWorkspaceKey().Returns(workspaceKey);
        UnitOfWork.Tasks.GetTasksAsync(workspaceKey).Returns(tasks);

        var result = await Service.GetTasks();

        result.Should().BeEquivalentTo(tasks);
    }

    [Fact]
    public async Task Update_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateProjectTaskRequest>()
            .Create();

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

        var result = await Service.Update(request);

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
        var request = Fixture
            .Build<UpdateProjectTaskRequest>()
            .Create();

        UnitOfWork.Tasks.GetAsync(request.Id).ReturnsNull();

        var result = await Service.Update(request);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Update_ShouldCallTransaction_WhenInputValid()
    {
        var request = Fixture
            .Build<UpdateProjectTaskRequest>()
            .Create();

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

        await Service.Update(request);

        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<MoveTasksToGroupRequest>()
            .Create();

        var group = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value).Returns(group);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());

        var result = await Service.MoveTasksToGroup(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<MoveTasksToGroupRequest>()
            .Create();

        var group = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value).Returns(group);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());

        await Service.MoveTasksToGroup(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task MoveTasksToGroup_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture
            .Build<MoveTasksToGroupRequest>()
            .Create();

        var group = AutoFixtures.BoardGroup;

        UnitOfWork.BoardGroups.GetAsync(request.NewGroupId!.Value).Returns(group);
        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());

        await Service.MoveTasksToGroup(request);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<MoveTaskActivityMeta>>>());
    }

    [Fact]
    public async Task ReassignTasks_ShouldReturnCorrectly_WhenInputValid()
    {
        var request = Fixture
            .Build<ReassignTasksRequest>()
            .Create();

        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        var result = await Service.ReassignTasks(request);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task ReassignTasks_ShouldCallCompleteAsync_WhenInputValid()
    {
        var request = Fixture
            .Build<ReassignTasksRequest>()
            .Create();

        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        await Service.ReassignTasks(request);

        await UnitOfWork.Received(1).CompleteAsync();
    }

    [Fact]
    public async Task ReassignTasks_ShouldLogActivity_WhenValidId()
    {
        var request = Fixture
            .Build<ReassignTasksRequest>()
            .Create();

        UnitOfWork.Tasks.GetTaskIdsInBoard(request.BoardId).Returns(new List<int>());
        UnitOfWork.Tasks.GetAllByIdAsync(Arg.Any<IEnumerable<int>>()).Returns(AutoFixtures.ProjectTasks);

        await Service.ReassignTasks(request);

        Activity.Received(1).LogWithMany(Arg.Any<Action<ActivityMultipleOptions<AssignActivityMeta>>>());
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

        var result = await Service.MoveTaskInBoardGroup(request);

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

        await Service.MoveTaskInBoardGroup(request);

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

        await Service.MoveTaskInBoardGroup(request);

        Activity.Received(1).LogWith(Arg.Any<Action<ActivityOptions<MoveTaskActivityMeta>>>());
    }

    [Fact]
    public async Task MoveTaskInBoardGroup_MoveTaskInGroup_ShouldReturnCorrectly_WhenInputValid()
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

        var result = await Service.MoveTaskInBoardGroup(request);

        result.IsSuccess.Should().BeTrue();
    }
}
