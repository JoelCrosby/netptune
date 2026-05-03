using FluentAssertions;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Services.Sprints.Commands;
using Netptune.Services.Sprints.Queries;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Sprints.Commands;

public class SprintCommandHandlerTests
{
    private const string WorkspaceKey = "workspace";

    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();

    public SprintCommandHandlerTests()
    {
        Identity.GetWorkspaceKey().Returns(WorkspaceKey);
        Identity.GetCurrentUser().Returns(new AppUser { Id = "user-1" });
    }

    [Fact]
    public async Task Create_ShouldAddPlanningSprint_WhenInputValid()
    {
        var handler = new CreateSprintCommandHandler(UnitOfWork, Identity, Activity);
        var workspace = CreateWorkspace();
        var project = CreateProject(workspace.Id);
        var request = new AddSprintRequest
        {
            Name = "  Sprint 1  ",
            Goal = "Ship sprint support",
            StartDate = new DateTime(2026, 05, 01),
            EndDate = new DateTime(2026, 05, 15),
            ProjectId = project.Id,
        };
        var sprintViewModel = CreateSprintDetailViewModel(name: "Sprint 1", projectId: project.Id, workspaceId: workspace.Id);
        Sprint? addedSprint = null;

        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, cancellationToken: TestContext.Current.CancellationToken).Returns(workspace);
        UnitOfWork.Projects.GetAsync(project.Id, true, TestContext.Current.CancellationToken).Returns(project);
        UnitOfWork.Sprints.AddAsync(Arg.Any<Sprint>(), TestContext.Current.CancellationToken)
            .Returns(callInfo =>
            {
                addedSprint = callInfo.Arg<Sprint>();
                addedSprint.Id = sprintViewModel.Id;
                return addedSprint;
            });
        UnitOfWork.Sprints.GetSprintDetailAsync(WorkspaceKey, sprintViewModel.Id, TestContext.Current.CancellationToken)
            .Returns(sprintViewModel);

        var result = await handler.Handle(new CreateSprintCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        result.Payload.Should().Be(sprintViewModel);
        addedSprint.Should().NotBeNull();
        addedSprint!.Name.Should().Be("Sprint 1");
        addedSprint.Status.Should().Be(SprintStatus.Planning);
        addedSprint.ProjectId.Should().Be(project.Id);
        addedSprint.WorkspaceId.Should().Be(workspace.Id);
        addedSprint.OwnerId.Should().Be("user-1");
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        CaptureLoggedActivityType().Should().Be(ActivityType.Create);
    }

    [Fact]
    public async Task Create_ShouldFail_WhenNameIsBlank()
    {
        var handler = new CreateSprintCommandHandler(UnitOfWork, Identity, Activity);
        var request = new AddSprintRequest
        {
            Name = " ",
            StartDate = new DateTime(2026, 05, 01),
            EndDate = new DateTime(2026, 05, 15),
            ProjectId = 10,
        };

        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(CreateWorkspace());

        var result = await handler.Handle(new CreateSprintCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Sprint name is required");
        await UnitOfWork.Sprints.DidNotReceive().AddAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>());
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Create_ShouldFail_WhenProjectIsOutsideWorkspace()
    {
        var handler = new CreateSprintCommandHandler(UnitOfWork, Identity, Activity);
        var request = new AddSprintRequest
        {
            Name = "Sprint 1",
            StartDate = new DateTime(2026, 05, 01),
            EndDate = new DateTime(2026, 05, 15),
            ProjectId = 10,
        };

        UnitOfWork.Workspaces.GetBySlug(WorkspaceKey, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(CreateWorkspace(id: 1));
        UnitOfWork.Projects.GetAsync(request.ProjectId, true, TestContext.Current.CancellationToken)
            .Returns(CreateProject(workspaceId: 2, id: request.ProjectId));

        var result = await handler.Handle(new CreateSprintCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Project with id 10 not found");
        await UnitOfWork.Sprints.DidNotReceive().AddAsync(Arg.Any<Sprint>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_ShouldActivatePlanningSprint_WhenNoOtherActiveSprintExists()
    {
        var handler = new StartSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Planning);
        var sprintViewModel = CreateSprintDetailViewModel(sprint.Id, sprint.Name, sprint.ProjectId, sprint.WorkspaceId, SprintStatus.Active);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);
        UnitOfWork.Sprints.HasActiveSprintAsync(sprint.ProjectId, sprint.Id, TestContext.Current.CancellationToken)
            .Returns(false);
        UnitOfWork.Sprints.GetSprintDetailAsync(WorkspaceKey, sprint.Id, TestContext.Current.CancellationToken)
            .Returns(sprintViewModel);

        var result = await handler.Handle(new StartSprintCommand(sprint.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        sprint.Status.Should().Be(SprintStatus.Active);
        sprint.ModifiedByUserId.Should().Be("user-1");
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        CaptureLoggedActivityType().Should().Be(ActivityType.ModifyStatus);
    }

    [Fact]
    public async Task Start_ShouldFail_WhenProjectAlreadyHasActiveSprint()
    {
        var handler = new StartSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Planning);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);
        UnitOfWork.Sprints.HasActiveSprintAsync(sprint.ProjectId, sprint.Id, TestContext.Current.CancellationToken)
            .Returns(true);

        var result = await handler.Handle(new StartSprintCommand(sprint.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("This project already has an active sprint");
        sprint.Status.Should().Be(SprintStatus.Planning);
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Complete_ShouldCompleteActiveSprint()
    {
        var handler = new CompleteSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Active);
        var sprintViewModel = CreateSprintDetailViewModel(sprint.Id, sprint.Name, sprint.ProjectId, sprint.WorkspaceId, SprintStatus.Completed);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);
        UnitOfWork.Sprints.GetSprintDetailAsync(WorkspaceKey, sprint.Id, TestContext.Current.CancellationToken)
            .Returns(sprintViewModel);

        var result = await handler.Handle(new CompleteSprintCommand(sprint.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        sprint.Status.Should().Be(SprintStatus.Completed);
        sprint.CompletedAt.Should().NotBeNull();
        sprint.ModifiedByUserId.Should().Be("user-1");
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        CaptureLoggedActivityType().Should().Be(ActivityType.ModifyStatus);
    }

    [Fact]
    public async Task AddTasks_ShouldAssignDistinctProjectTasksToSprint()
    {
        var handler = new AddTasksToSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Planning);
        var firstTask = CreateTask(id: 10, workspaceId: sprint.WorkspaceId, projectId: sprint.ProjectId);
        var secondTask = CreateTask(id: 11, workspaceId: sprint.WorkspaceId, projectId: sprint.ProjectId);
        var request = new AddTasksToSprintRequest { TaskIds = [firstTask.Id, firstTask.Id, secondTask.Id] };
        var sprintViewModel = CreateSprintDetailViewModel(sprint.Id, sprint.Name, sprint.ProjectId, sprint.WorkspaceId);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);
        UnitOfWork.Tasks.GetAsync(firstTask.Id, cancellationToken: TestContext.Current.CancellationToken).Returns(firstTask);
        UnitOfWork.Tasks.GetAsync(secondTask.Id, cancellationToken: TestContext.Current.CancellationToken).Returns(secondTask);
        UnitOfWork.Sprints.GetSprintDetailAsync(WorkspaceKey, sprint.Id, TestContext.Current.CancellationToken)
            .Returns(sprintViewModel);

        var result = await handler.Handle(new AddTasksToSprintCommand(sprint.Id, request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        firstTask.SprintId.Should().Be(sprint.Id);
        secondTask.SprintId.Should().Be(sprint.Id);
        await UnitOfWork.Tasks.Received(1).GetAsync(firstTask.Id, cancellationToken: TestContext.Current.CancellationToken);
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        CaptureLoggedActivityType().Should().Be(ActivityType.Assign);
    }

    [Fact]
    public async Task AddTasks_ShouldFail_WhenTaskProjectDoesNotMatchSprintProject()
    {
        var handler = new AddTasksToSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Planning, projectId: 20);
        var task = CreateTask(id: 10, workspaceId: sprint.WorkspaceId, projectId: 21);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);
        UnitOfWork.Tasks.GetAsync(task.Id, cancellationToken: TestContext.Current.CancellationToken).Returns(task);

        var result = await handler.Handle(
            new AddTasksToSprintCommand(sprint.Id, new AddTasksToSprintRequest { TaskIds = [task.Id] }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Task with id 10 is not in sprint project");
        task.SprintId.Should().BeNull();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RemoveTask_ShouldClearTaskSprintId_WhenTaskIsInSprint()
    {
        var handler = new RemoveTaskFromSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Active);
        var task = CreateTask(id: 10, workspaceId: sprint.WorkspaceId, projectId: sprint.ProjectId, sprintId: sprint.Id);
        var sprintViewModel = CreateSprintDetailViewModel(sprint.Id, sprint.Name, sprint.ProjectId, sprint.WorkspaceId, SprintStatus.Active);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);
        UnitOfWork.Tasks.GetAsync(task.Id, cancellationToken: TestContext.Current.CancellationToken).Returns(task);
        UnitOfWork.Sprints.GetSprintDetailAsync(WorkspaceKey, sprint.Id, TestContext.Current.CancellationToken)
            .Returns(sprintViewModel);

        var result = await handler.Handle(new RemoveTaskFromSprintCommand(sprint.Id, task.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        task.SprintId.Should().BeNull();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        CaptureLoggedActivityType().Should().Be(ActivityType.Unassign);
    }

    [Fact]
    public async Task Delete_ShouldSoftDeletePlanningSprint_AndClearTasks()
    {
        var handler = new DeleteSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Planning);
        var task = CreateTask(id: 10, workspaceId: sprint.WorkspaceId, projectId: sprint.ProjectId, sprintId: sprint.Id);
        sprint.ProjectTasks = [task];

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);

        var result = await handler.Handle(new DeleteSprintCommand(sprint.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        sprint.IsDeleted.Should().BeTrue();
        sprint.DeletedByUserId.Should().Be("user-1");
        task.SprintId.Should().BeNull();
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        CaptureLoggedActivityType().Should().Be(ActivityType.Delete);
    }

    [Fact]
    public async Task Delete_ShouldFail_WhenSprintIsActive()
    {
        var handler = new DeleteSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Active);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);

        var result = await handler.Handle(new DeleteSprintCommand(sprint.Id), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Only planning or cancelled sprints can be deleted");
        sprint.IsDeleted.Should().BeFalse();
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ShouldApplyEditableFields_WhenInputValid()
    {
        var handler = new UpdateSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Planning);
        var request = new UpdateSprintRequest
        {
            Id = sprint.Id,
            Name = "  Renamed sprint  ",
            Goal = "New goal",
            StartDate = new DateTime(2026, 06, 01),
            EndDate = new DateTime(2026, 06, 15),
            Status = SprintStatus.Active,
        };
        var sprintViewModel = CreateSprintDetailViewModel(sprint.Id, "Renamed sprint", sprint.ProjectId, sprint.WorkspaceId, SprintStatus.Active);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);
        UnitOfWork.Sprints.GetSprintDetailAsync(WorkspaceKey, sprint.Id, TestContext.Current.CancellationToken)
            .Returns(sprintViewModel);

        var result = await handler.Handle(new UpdateSprintCommand(request), TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeTrue();
        sprint.Name.Should().Be("Renamed sprint");
        sprint.Goal.Should().Be("New goal");
        sprint.StartDate.Should().Be(request.StartDate);
        sprint.EndDate.Should().Be(request.EndDate);
        sprint.Status.Should().Be(SprintStatus.Active);
        sprint.ModifiedByUserId.Should().Be("user-1");
        await UnitOfWork.Received(1).CompleteAsync(TestContext.Current.CancellationToken);
        CaptureLoggedActivityType().Should().Be(ActivityType.Modify);
    }

    [Fact]
    public async Task Update_ShouldFail_WhenCompletedSprintIsEditedWithoutCancelling()
    {
        var handler = new UpdateSprintCommandHandler(UnitOfWork, Identity, Activity);
        var sprint = CreateSprint(status: SprintStatus.Completed);

        UnitOfWork.Sprints.GetSprintInWorkspaceAsync(WorkspaceKey, sprint.Id, cancellationToken: TestContext.Current.CancellationToken)
            .Returns(sprint);

        var result = await handler.Handle(
            new UpdateSprintCommand(new UpdateSprintRequest { Id = sprint.Id, Name = "Nope" }),
            TestContext.Current.CancellationToken);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().Be("Completed sprints cannot be edited");
        await UnitOfWork.DidNotReceive().CompleteAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetSprint_ShouldReturnNotFound_WhenSprintDoesNotExist()
    {
        var handler = new GetSprintQueryHandler(UnitOfWork, Identity);

        UnitOfWork.Sprints.GetSprintDetailAsync(WorkspaceKey, 10, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await handler.Handle(new GetSprintQuery(10), TestContext.Current.CancellationToken);

        result.IsNotFound.Should().BeTrue();
    }

    private static Workspace CreateWorkspace(int id = 1)
    {
        return new Workspace
        {
            Id = id,
            Slug = WorkspaceKey,
        };
    }

    private static Project CreateProject(int workspaceId, int id = 10, bool isDeleted = false)
    {
        return new Project
        {
            Id = id,
            WorkspaceId = workspaceId,
            Name = "Project",
            Key = "PROJ",
            IsDeleted = isDeleted,
        };
    }

    private static Sprint CreateSprint(
        SprintStatus status,
        int id = 30,
        int workspaceId = 1,
        int projectId = 10)
    {
        return new Sprint
        {
            Id = id,
            Name = "Sprint",
            Status = status,
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            StartDate = new DateTime(2026, 05, 01),
            EndDate = new DateTime(2026, 05, 15),
            ProjectTasks = new List<ProjectTask>(),
        };
    }

    private static ProjectTask CreateTask(
        int id,
        int workspaceId,
        int projectId,
        int? sprintId = null)
    {
        return new ProjectTask
        {
            Id = id,
            Name = $"Task {id}",
            WorkspaceId = workspaceId,
            ProjectId = projectId,
            SprintId = sprintId,
        };
    }

    private static SprintDetailViewModel CreateSprintDetailViewModel(
        int id = 30,
        string name = "Sprint",
        int projectId = 10,
        int workspaceId = 1,
        SprintStatus status = SprintStatus.Planning)
    {
        return new SprintDetailViewModel
        {
            Id = id,
            Name = name,
            Status = status,
            StartDate = new DateTime(2026, 05, 01),
            EndDate = new DateTime(2026, 05, 15),
            ProjectId = projectId,
            ProjectName = "Project",
            ProjectKey = "PROJ",
            WorkspaceId = workspaceId,
        };
    }

    private ActivityType CaptureLoggedActivityType()
    {
        var call = Activity.ReceivedCalls().Single(call => call.GetMethodInfo().Name == nameof(IActivityLogger.Log));
        var action = (Action<ActivityOptions>)call.GetArguments()[0]!;
        var options = new ActivityOptions();
        action(options);

        return options.Type;
    }
}
