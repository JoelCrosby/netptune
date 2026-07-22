using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;

using Xunit;

namespace Netptune.Automation.Tests;

public sealed class AutomationExecutionServiceTests
{
    private readonly AutomationTestFixture Fixture;

    public AutomationExecutionServiceTests(AutomationTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_creates_run_and_flag_for_matching_status_rule()
    {
        await using var scope = await Fixture.CreateScope();
        var scenario = await AutomationTestData.CreateScenario(scope.Db, "in-progress");
        var newStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "new");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            "in-progress");

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, newStatusId, inProgressStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);
        var flag = await scope.Db.Flags.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(rule.Id);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
        run.EntityType.Should().Be(EntityType.Task);
        run.EntityId.Should().Be(scenario.Task.Id);

        flag.AutomationRuleId.Should().Be(rule.Id);
        flag.EntityType.Should().Be(EntityType.Task);
        flag.EntityId.Should().Be(scenario.Task.Id);
        flag.Name.Should().Be("Needs attention");
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_is_idempotent_for_same_event()
    {
        await using var scope = await Fixture.CreateScope();
        var scenario = await AutomationTestData.CreateScenario(scope.Db, "complete");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            "complete");
        var eventId = Guid.NewGuid();
        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = eventId,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, inProgressStatusId, completeStatusId),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);
        var flagCount = await scope.Db.Flags.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
        flagCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_creates_run_for_matching_name_rule()
    {
        await using var scope = await Fixture.CreateScope();
        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskChangedRule(scope.Db, scenario, [TaskChangeField.Name]);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(rule.Id);
        run.TriggerType.Should().Be(AutomationTriggerType.TaskChanged);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_updates_task_for_matching_rule()
    {
        await using var scope = await Fixture.CreateScope();
        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            actionType: AutomationActionType.UpdateTask);

        var configuredStatusId = await scope.Db.AutomationActions
            .Where(action => action.Type == AutomationActionType.UpdateTask)
            .Select(action => action.Config!.RootElement.GetProperty("statusId").GetInt32())
            .SingleAsync(TestContext.Current.CancellationToken);
        var expectedStatusId = await scope.Db.Statuses
            .Where(status => status.WorkspaceId == scenario.Workspace.Id && status.Key == "complete")
            .Select(status => status.Id)
            .SingleAsync(TestContext.Current.CancellationToken);

        configuredStatusId.Should().Be(expectedStatusId);
        scope.Db.ChangeTracker.Clear();

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        }, TestContext.Current.CancellationToken);

        scope.Db.ChangeTracker.Clear();
        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);
        run.Status.Should().Be(AutomationRunStatus.Succeeded, run.Message);

        var task = await scope.Db.ProjectTasks
            .Include(task => task.Status)
            .SingleAsync(TestContext.Current.CancellationToken);

        task.Priority.Should().Be(TaskPriority.High);
        task.StatusId.Should().Be(expectedStatusId);
        task.Status.Key.Should().Be("complete");
        task.ModifiedByUserId.Should().Be(scenario.Owner.Id);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_adds_comment_for_matching_rule()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            actionType: AutomationActionType.AddComment);

        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Name, "Old name", "New name"),
            ],
        };

        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteTaskChangedRules(message, TestContext.Current.CancellationToken);

        var comment = await scope.Db.Comments.SingleAsync(TestContext.Current.CancellationToken);
        var commentEvent = scope.EventRecords.Events.Single(record => record.EventKey == EventKeys.CommentCreated);
        var payload = commentEvent.Payload.Should().BeOfType<CommentEventPayload>().Subject;

        comment.Body.Should().Be("Added by test automation");
        comment.EntityType.Should().Be(EntityType.Task);
        comment.EntityId.Should().Be(scenario.Task.Id);
        comment.WorkspaceId.Should().Be(scenario.Workspace.Id);
        comment.OwnerId.Should().Be(scenario.Owner.Id);
        commentEvent.SubjectType.Should().Be(EventEntityTypes.From(EntityType.Task));
        commentEvent.SubjectId.Should().Be(scenario.Task.Id.ToString());
        commentEvent.ActorUserId.Should().Be(scenario.Owner.Id);
        payload.CommentId.Should().Be(comment.Id);
        payload.RecipientUserIds.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_supports_status_changed_rules()
    {
        await using var scope = await Fixture.CreateScope();
        var scenario = await AutomationTestData.CreateScenario(scope.Db, "complete");
        var inProgressStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");
        var rule = await AutomationTestData.CreateStatusChangedRule(scope.Db, scenario, "complete");

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, inProgressStatusId, completeStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(rule.Id);
        run.TriggerType.Should().Be(AutomationTriggerType.TaskStatusChanged);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task ExecuteUnassignedRules_creates_notification_for_eligible_unassigned_task()
    {
        await using var scope = await Fixture.CreateScope();
        var scenario = await AutomationTestData.CreateScenario(
            scope.Db,
            assignTask: false,
            taskUpdatedAt: DateTime.UtcNow.AddDays(-3));
        await AutomationTestData.CreateUnassignedRule(
            scope.Db,
            scenario,
            durationDays: 2,
            AutomationActionType.NotifyTaskAssignees);

        await scope.AutomationExecution.ExecuteUnassignedRules(TestContext.Current.CancellationToken);

        var notification = await scope.Db.Notifications.SingleAsync(TestContext.Current.CancellationToken);
        var activityLog = await scope.Db.EventRecords.SingleAsync(TestContext.Current.CancellationToken);
        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        notification.UserId.Should().Be(scenario.Owner.Id);
        notification.EventRecordId.Should().Be(activityLog.Id);
        notification.Link.Should().Be($"/{scenario.Workspace.Slug}/tasks/{scenario.Project.Key}-{scenario.Task.ProjectScopeId}");
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task ExecuteDueDateRules_creates_run_for_task_due_on_configured_day()
    {
        await using var scope = await Fixture.CreateScope();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3);
        var scenario = await AutomationTestData.CreateScenario(scope.Db, dueDate: dueDate);
        var rule = await AutomationTestData.CreateDueDateRule(scope.Db, scenario, durationDays: 3);

        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);
        var flag = await scope.Db.Flags.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(rule.Id);
        run.TriggerType.Should().Be(AutomationTriggerType.TaskDueDateApproaching);
        run.IdempotencyKey.Should().EndWith($":due:{dueDate:yyyy-MM-dd}");
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
        flag.EntityId.Should().Be(scenario.Task.Id);
    }

    [Fact]
    public async Task ExecuteDueDateRules_is_idempotent_for_same_due_date()
    {
        await using var scope = await Fixture.CreateScope();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var scenario = await AutomationTestData.CreateScenario(scope.Db, dueDate: dueDate);
        await AutomationTestData.CreateDueDateRule(scope.Db, scenario, durationDays: 0);

        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);
        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);
        var flagCount = await scope.Db.Flags.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(1);
        flagCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteDueDateRules_does_not_run_before_configured_day()
    {
        await using var scope = await Fixture.CreateScope();
        var dueDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(4);
        var scenario = await AutomationTestData.CreateScenario(scope.Db, dueDate: dueDate);
        await AutomationTestData.CreateDueDateRule(scope.Db, scenario, durationDays: 3);

        await scope.AutomationExecution.ExecuteDueDateRules(TestContext.Current.CancellationToken);

        var runCount = await scope.Db.AutomationRuns.CountAsync(TestContext.Current.CancellationToken);
        var flagCount = await scope.Db.Flags.CountAsync(TestContext.Current.CancellationToken);

        runCount.Should().Be(0);
        flagCount.Should().Be(0);
    }
}
