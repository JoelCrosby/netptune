using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.Logging;

using Netptune.Core.Enums;
using Netptune.Core.Entities;
using Netptune.Core.Models.Activity;
using Netptune.Core.Requests;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Services.Tasks.Commands;

using NSubstitute;
using NSubstitute.ReturnsExtensions;

using Xunit;

namespace Netptune.UnitTests.Netptune.Services.Tasks.Commands;

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

    private ProjectTask BuildTask(TaskPriority? priority = null, EstimateType? estimateType = null, decimal? estimateValue = null)
    {
        return Fixture.Build<ProjectTask>()
            .Without(p => p.ProjectTaskAppUsers)
            .Without(p => p.Project)
            .Without(p => p.ProjectTaskInBoardGroups)
            .Without(p => p.ProjectTaskTags)
            .Without(p => p.Tags)
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
        Activity.When(a => a.Log(Arg.Any<Action<ActivityOptions>>()))
            .Do(callInfo =>
            {
                var opts = new ActivityOptions();
                callInfo.Arg<Action<ActivityOptions>>().Invoke(opts);
                types.Add(opts.Type);
            });
        return types;
    }

    private void SetupHandlerDependencies(UpdateProjectTaskRequest request, ProjectTask task, TaskViewModel viewModel)
    {
        UnitOfWork.Tasks.GetAsync(request.Id).Returns(task);
        UnitOfWork.Tasks.GetTaskViewModel(Arg.Any<int>()).Returns(viewModel);
        UnitOfWork.BoardGroups.GetBoardGroupsForProjectTask(Arg.Any<int>()).Returns(AutoFixtures.BoardGroups);
        UnitOfWork.Boards.GetDefaultBoardInProject(Arg.Any<int>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(AutoFixtures.Board);
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

        var result = await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

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
        UnitOfWork.Tasks.GetAsync(request.Id, cancellationToken: TestContext.Current.CancellationToken).ReturnsNull();

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

        SetupHandlerDependencies(request, task, viewModel);

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

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

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

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

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

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

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

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

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

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

        await Handler.Handle(new UpdateTaskCommand(request), CancellationToken.None);

        loggedTypes.Should().NotContain(ActivityType.ModifyEstimate);
    }
}
