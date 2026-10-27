using System.Text.Json;

using FluentAssertions;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.Sprints.Commands;
using Netptune.Handlers.Tasks.Commands;
using Netptune.Handlers.Tasks.Queries;

using NSubstitute;

using Xunit;

namespace Netptune.UnitTests.Netptune.Ai;

public class AiTaskUndoTests
{
    private const int TaskId = 42;

    private readonly IMediator Mediator = Substitute.For<IMediator>();

    public AiTaskUndoTests()
    {
        Mediator
            .Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<TaskViewModel>.Success(CreateTask()));
    }

    [Fact]
    public async Task Restore_ShouldPutBackOnlyWhatTheChangeChanged_LeavingLaterEditsAlone()
    {
        var before = CreateTask(assignee: "bob");
        var applied = CreateTask(assignee: "alice");
        var current = CreateTask(assignee: "alice") with { Name = "Renamed by Alice", StatusId = 9 };

        var result = await Restore(before, applied, current);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<UpdateTaskCommand>(command =>
                command.Request.AssigneeIds!.SequenceEqual(new[] { "bob" }) &&
                command.Request.Name == null &&
                command.Request.StatusId == null &&
                command.Request.Tags == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_ShouldRefuse_WhenWhatTheChangeChangedWasEditedSince()
    {
        var before = CreateTask(assignee: "bob");
        var applied = CreateTask(assignee: "alice");
        var current = CreateTask(assignee: "carol");

        var result = await Restore(before, applied, current);

        result.Status.Should().Be(AiChangeApplyStatus.Failed);
        await Mediator.DidNotReceive().Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_ShouldClearAnEstimateAndPriority_ThatTheChangeAdded()
    {
        var before = CreateTask();
        var applied = CreateTask() with
        {
            Priority = TaskPriority.High,
            EstimateType = EstimateType.StoryPoints,
            EstimateValue = 5,
        };

        var result = await Restore(before, applied, applied);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<UpdateTaskCommand>(command => command.Request.ClearEstimate && command.Request.ClearPriority),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restore_ShouldOnlyMoveTheSprint_WhenThatIsAllTheChangeDid()
    {
        var before = CreateTask() with { SprintId = 3 };
        var applied = CreateTask() with { SprintId = 4 };

        Mediator
            .Send(Arg.Any<AddTasksToSprintCommand>(), Arg.Any<CancellationToken>())
            .Returns(ClientResponse<SprintDetailViewModel>.Success(new SprintDetailViewModel()));

        var result = await Restore(before, applied, applied);

        result.Status.Should().Be(AiChangeApplyStatus.Applied);
        await Mediator.Received(1).Send(
            Arg.Is<AddTasksToSprintCommand>(command => command.SprintId == 3),
            Arg.Any<CancellationToken>());
        await Mediator.DidNotReceive().Send(Arg.Any<UpdateTaskCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CaptureApplied_ShouldRecordTheTaskAsTheChangeLeftIt()
    {
        GivenTask(CreateTask(assignee: "bob"));

        var change = CreateChange(await AiTaskUndo.Capture(Mediator, TaskId, TestContext.Current.CancellationToken));

        GivenTask(CreateTask(assignee: "alice"));

        var context = new AiChangeApplyContext { Change = change, ResolvedRefs = new Dictionary<string, int>() };
        var payload = await AiTaskUndo.CaptureApplied(Mediator, context, TestContext.Current.CancellationToken);
        var snapshot = payload!.Deserialize<AiTaskSnapshot>(new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        snapshot.AssigneeIds.Should().Equal("bob");
        snapshot.Applied!.AssigneeIds.Should().Equal("alice");
    }

    [Theory]
    [InlineData(typeof(AssignTaskChangeHandler), NetptunePermissions.Tasks.Reassign)]
    [InlineData(typeof(SetTaskTagsChangeHandler), NetptunePermissions.Tags.Assign)]
    [InlineData(typeof(MoveTaskToSprintChangeHandler), NetptunePermissions.Sprints.ManageTasks)]
    [InlineData(typeof(RemoveTaskFromSprintChangeHandler), NetptunePermissions.Sprints.ManageTasks)]
    [InlineData(typeof(MoveTaskToBoardGroupChangeHandler), NetptunePermissions.Tasks.Move)]
    [InlineData(typeof(UpdateTaskChangeHandler), NetptunePermissions.Tasks.Update)]
    public void UndoPermissions_ShouldMatchWhatApplyingTheChangeNeeds(Type handlerType, string permission)
    {
        var handler = (IAiChangeUndoHandler)Activator.CreateInstance(handlerType, Mediator)!;

        handler.UndoPermissions.Should().BeEquivalentTo([permission]);
    }

    private async Task<AiAppliedChangeResult> Restore(
        TaskViewModel before,
        TaskViewModel applied,
        TaskViewModel current)
    {
        GivenTask(before);

        var change = CreateChange(await AiTaskUndo.Capture(Mediator, TaskId, TestContext.Current.CancellationToken));

        GivenTask(applied);

        var context = new AiChangeApplyContext { Change = change, ResolvedRefs = new Dictionary<string, int>() };

        change.UndoPayload = await AiTaskUndo.CaptureApplied(Mediator, context, TestContext.Current.CancellationToken);

        GivenTask(current);
        Mediator.ClearReceivedCalls();

        return await AiTaskUndo.Restore(
            Mediator,
            new AiChangeUndoContext { Change = change },
            TestContext.Current.CancellationToken);
    }

    private void GivenTask(TaskViewModel task)
    {
        Mediator
            .Send(Arg.Is<GetTaskQuery>(query => query.Id == TaskId), Arg.Any<CancellationToken>())
            .Returns(task);
    }

    private static TaskViewModel CreateTask(string? assignee = null)
    {
        var assignees = assignee is null
            ? []
            : new List<AssigneeViewModel> { new() { Id = assignee, DisplayName = assignee } };

        return new TaskViewModel
        {
            Id = TaskId,
            Name = "Draft the brief",
            Description = "Notes",
            StatusId = 2,
            Assignees = assignees,
        };
    }

    private static AiProposedChange CreateChange(JsonDocument? undoPayload)
    {
        return new AiProposedChange
        {
            Id = 1,
            ChangeSetId = Guid.NewGuid(),
            Sequence = 1,
            ToolName = "propose_assign_task",
            EntityType = "task",
            EntityId = TaskId,
            Summary = "Assign a task",
            Payload = JsonSerializer.SerializeToDocument(new { taskId = TaskId }),
            UndoPayload = undoPayload,
            ValidationStatus = AiChangeValidationStatus.Valid,
            ApplyStatus = AiChangeApplyStatus.Applied,
        };
    }
}
