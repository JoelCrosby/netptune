using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Enums;
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
        var scenario = await AutomationTestData.CreateScenario(scope.Db, ProjectTaskStatus.InProgress);
        var rule = await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            ProjectTaskStatus.InProgress);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = Guid.NewGuid(),
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, ProjectTaskStatus.New, ProjectTaskStatus.InProgress),
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
        var scenario = await AutomationTestData.CreateScenario(scope.Db, ProjectTaskStatus.Complete);
        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Status],
            ProjectTaskStatus.Complete);
        var eventId = Guid.NewGuid();
        var message = new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            EventId = eventId,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, ProjectTaskStatus.InProgress, ProjectTaskStatus.Complete),
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
        var scenario = await AutomationTestData.CreateScenario(scope.Db, ProjectTaskStatus.New);
        await AutomationTestData.CreateTaskChangedRule(
            scope.Db,
            scenario,
            [TaskChangeField.Name],
            actionType: AutomationActionType.UpdateTask);

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

        var task = await scope.Db.ProjectTasks.SingleAsync(TestContext.Current.CancellationToken);
        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        task.Status.Should().Be(ProjectTaskStatus.Complete);
        task.Priority.Should().Be(TaskPriority.High);
        task.ModifiedByUserId.Should().Be(scenario.Owner.Id);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task ExecuteTaskChangedRules_supports_legacy_status_changed_rules()
    {
        await using var scope = await Fixture.CreateScope();
        var scenario = await AutomationTestData.CreateScenario(scope.Db, ProjectTaskStatus.Complete);
        var rule = await AutomationTestData.CreateStatusChangedRule(scope.Db, scenario, ProjectTaskStatus.Complete);

        await scope.AutomationExecution.ExecuteTaskChangedRules(new TaskChangedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, ProjectTaskStatus.InProgress, ProjectTaskStatus.Complete),
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
        var activityLog = await scope.Db.ActivityLogs.SingleAsync(TestContext.Current.CancellationToken);
        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        notification.UserId.Should().Be(scenario.Owner.Id);
        notification.ActivityLogId.Should().Be(activityLog.Id);
        notification.Link.Should().Be($"/{scenario.Workspace.Slug}/tasks/{scenario.Project.Key}-{scenario.Task.ProjectScopeId}");
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }
}
