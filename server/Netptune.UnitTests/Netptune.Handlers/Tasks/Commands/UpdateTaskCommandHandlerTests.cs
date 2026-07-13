using AutoFixture;

using FluentAssertions;

using Netptune.Core.Enums;
using Netptune.Core.Entities;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Tasks.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Handlers.Tasks.Commands;

public class UpdateTaskCommandHandlerTests
{
    private readonly Fixture Fixture = new();
    private readonly UpdateTaskCommandHandler Handler;
    private readonly INetptuneUnitOfWork UnitOfWork = Substitute.For<INetptuneUnitOfWork>();
    private readonly IActivityLogger Activity = Substitute.For<IActivityLogger>();
    private readonly IEventPublisher EventPublisher = Substitute.For<IEventPublisher>();
    private readonly IIdentityService Identity = Substitute.For<IIdentityService>();

    public UpdateTaskCommandHandlerTests()
    {
        Identity.GetCurrentUserId().Returns("user-1");
        Handler = new(UnitOfWork, Activity, EventPublisher, Identity);
    }

    private ProjectTask BuildTask(TaskPriority? priority = null, EstimateType? estimateType = null, decimal? estimateValue = null)
    {
        return Fixture.Build<ProjectTask>()
            .Without(p => p.ProjectTaskAppUsers)
            .Without(p => p.Project)
            .Without(p => p.ProjectTaskInBoardGroups)
            .Without(p => p.ProjectTaskTags)
            .Without(p => p.Tags)
            .Without(p => p.Sprint)
            .With(p => p.Status, AutoFixtures.TaskStatus)
            .With(p => p.Workspace, AutoFixtures.Workspace)
            .With(p => p.Priority, priority)
            .With(p => p.EstimateType, estimateType)
            .With(p => p.EstimateValue, estimateValue)
            .WithoutAuditable()
            .Create();
    }

    private List<ActivityType> CaptureLoggedActivityTypes()
    {
        var types = new List<ActivityType>();
        Activity.When(a => a.LogChanges(Arg.Any<Action<ActivityChangeSetOptions>>()))
            .Do(callInfo =>
            {
                var opts = new ActivityChangeSetOptions();
                callInfo.Arg<Action<ActivityChangeSetOptions>>().Invoke(opts);
                types.AddRange(opts.Changes.Select(change => change.Type));
            });
        return types;
    }

    private void SetupHandlerDependencies(UpdateProjectTaskRequest request, ProjectTask task, TaskViewModel viewModel)
    {
        var oldViewModel = new TaskViewModel
        {
            Id = task.Id,
            Name = task.Name,
            Description = task.Description,
            StatusId = task.StatusId,
            ProjectId = task.ProjectId,
            Priority = task.Priority,
            EstimateType = task.EstimateType,
            EstimateValue = task.EstimateValue,
        };

        UnitOfWork.Tasks.GetTaskForUpdate(request.Id, Arg.Any<CancellationToken>()).Returns(task);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>(), Arg.Any<CancellationToken>()).Returns(oldViewModel, viewModel);
        UnitOfWork.InvokeTransaction();
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
            Priority = request.Priority,
            EstimateType = request.EstimateType,
            EstimateValue = request.EstimateValue,
        };

        SetupHandlerDependencies(request, task, viewModel);

        var result = await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        result.Payload.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(request.Name);
        result.Payload.Description.Should().Be(request.Description);
        result.Payload.SortOrder.Should().Be(request.SortOrder);
        result.Payload.Priority.Should().Be(request.Priority);
        result.Payload.EstimateType.Should().Be(request.EstimateType);
        result.Payload.EstimateValue.Should().Be(request.EstimateValue);
    }

    [Fact]
    public async Task Update_ShouldReturnFailed_WhenIdNotFound()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        UnitOfWork.Tasks.GetTaskViewModel(request.Id, TestContext.Current.CancellationToken).ReturnsNull();

        var result = await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

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

        SetupHandlerDependencies(request, task, viewModel);

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        await UnitOfWork.Received(1).Transaction(Arg.Any<Func<Task>>());
    }

    [Fact]
    public async Task Update_ShouldLogModifyPriority_WhenPriorityChanges()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = BuildTask(priority: TaskPriority.Low);
        var viewModel = new TaskViewModel { Name = task.Name, Priority = TaskPriority.High };

        SetupHandlerDependencies(request, task, viewModel);
        var loggedTypes = CaptureLoggedActivityTypes();

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        loggedTypes.Should().Contain(ActivityType.ModifyPriority);
    }

    [Fact]
    public async Task Update_ShouldNotLogModifyPriority_WhenPriorityUnchanged()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = BuildTask(priority: TaskPriority.Medium);
        var viewModel = new TaskViewModel { Name = task.Name, Priority = TaskPriority.Medium };

        SetupHandlerDependencies(request, task, viewModel);
        var loggedTypes = CaptureLoggedActivityTypes();

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        loggedTypes.Should().NotContain(ActivityType.ModifyPriority);
    }

    [Fact]
    public async Task Update_ShouldLogModifyEstimate_WhenEstimateTypeChanges()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = BuildTask(estimateType: EstimateType.Hours, estimateValue: 3);
        var viewModel = new TaskViewModel
        {
            Name = task.Name,
            EstimateType = EstimateType.StoryPoints,
            EstimateValue = 3,
        };

        SetupHandlerDependencies(request, task, viewModel);
        var loggedTypes = CaptureLoggedActivityTypes();

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        loggedTypes.Should().Contain(ActivityType.ModifyEstimate);
    }

    [Fact]
    public async Task Update_ShouldLogModifyEstimate_WhenEstimateValueChanges()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = BuildTask(estimateType: EstimateType.StoryPoints, estimateValue: 3);
        var viewModel = new TaskViewModel
        {
            Name = task.Name,
            EstimateType = EstimateType.StoryPoints,
            EstimateValue = 8,
        };

        SetupHandlerDependencies(request, task, viewModel);
        var loggedTypes = CaptureLoggedActivityTypes();

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        loggedTypes.Should().Contain(ActivityType.ModifyEstimate);
    }

    [Fact]
    public async Task Update_ShouldNotLogModifyEstimate_WhenEstimateUnchanged()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = BuildTask(estimateType: EstimateType.StoryPoints, estimateValue: 5);
        var viewModel = new TaskViewModel
        {
            Name = task.Name,
            EstimateType = EstimateType.StoryPoints,
            EstimateValue = 5,
        };

        SetupHandlerDependencies(request, task, viewModel);
        var loggedTypes = CaptureLoggedActivityTypes();

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        loggedTypes.Should().NotContain(ActivityType.ModifyEstimate);
    }

    [Fact]
    public async Task Update_ShouldLogOneChangeSet_WhenSeveralFieldsChange()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>().Create();
        var task = BuildTask(priority: TaskPriority.Low, estimateType: EstimateType.Hours, estimateValue: 3);
        task.Name = "Original name";
        var viewModel = new TaskViewModel
        {
            Id = task.Id,
            Name = "Updated name",
            Priority = TaskPriority.High,
            EstimateType = EstimateType.Hours,
            EstimateValue = 3,
        };

        SetupHandlerDependencies(request, task, viewModel);
        var loggedTypes = CaptureLoggedActivityTypes();

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        Activity.Received(1).LogChanges(Arg.Any<Action<ActivityChangeSetOptions>>());
        Activity.DidNotReceive().Log(Arg.Any<Action<ActivityOptions>>());
        loggedTypes.Should().Contain([ActivityType.ModifyName, ActivityType.ModifyPriority]);
    }

    [Fact]
    public async Task Update_ShouldDispatchTaskChangedEvent_WhenNameChanges()
    {
        var request = Fixture.Build<UpdateProjectTaskRequest>()
            .With(req => req.Name, "Updated name")
            .Create();
        var task = BuildTask();
        task.Name = "Original name";
        var viewModel = new TaskViewModel
        {
            Id = task.Id,
            Name = "Updated name",
            Description = task.Description,
            StatusId = task.StatusId,
            WorkspaceId = 123,
        };

        SetupHandlerDependencies(request, task, viewModel);

        await Handler.Handle(new UpdateTaskCommand(request), TestContext.Current.CancellationToken);

        await EventPublisher.Received(1).Dispatch(Arg.Is<TaskChangedMessage>(message =>
            message.WorkspaceId == 123 &&
            message.TaskId == task.Id &&
            message.ActorUserId == "user-1" &&
            message.Changes.Any(change =>
                change.Field == TaskChangeField.Name &&
                change.OldValue == "Original name" &&
                change.NewValue == "Updated name")));
    }
}
