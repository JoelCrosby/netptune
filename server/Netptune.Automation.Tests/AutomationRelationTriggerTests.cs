using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Enums;
using Netptune.Core.Events.Relations;
using Netptune.Core.Events.Sprints;
using Netptune.Core.Events.Tasks;

using Xunit;

namespace Netptune.Automation.Tests;

[Collection("automation-database")]
public sealed class AutomationRelationTriggerTests
{
    private readonly AutomationTestFixture Fixture;

    public AutomationRelationTriggerTests(AutomationTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task SprintStarted_flags_every_task_in_the_sprint()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var sprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint One");
        var secondTask = await AutomationTestData.CreateTask(scope.Db, scenario, "Second Task");

        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, sprint.Id);
        await AutomationTestData.AssignTaskToSprint(scope.Db, secondTask.Id, sprint.Id);
        await AutomationTestData.CreateTaskStateRule(scope.Db, scenario, AutomationTriggerType.SprintStarted);

        await scope.AutomationExecution.ExecuteEventRules(new SprintLifecycleMessage
        {
            WorkspaceId = scenario.Workspace.Id,
            SprintId = sprint.Id,
            State = SprintLifecycleState.Started,
            ActorUserId = scenario.Owner.Id,
        }, TestContext.Current.CancellationToken);

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().HaveCount(2);
        runs.Select(run => run.EntityId).Should().BeEquivalentTo([scenario.Task.Id, secondTask.Id]);
        runs.Should().OnlyContain(run => run.Status == AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task SprintCompleted_ignores_sprint_started_rules()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var sprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint One");

        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, sprint.Id);
        await AutomationTestData.CreateTaskStateRule(scope.Db, scenario, AutomationTriggerType.SprintStarted);

        await scope.AutomationExecution.ExecuteEventRules(new SprintLifecycleMessage
        {
            WorkspaceId = scenario.Workspace.Id,
            SprintId = sprint.Id,
            State = SprintLifecycleState.Completed,
            ActorUserId = scenario.Owner.Id,
        }, TestContext.Current.CancellationToken);

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().BeEmpty();
    }

    [Fact]
    public async Task TaskBlocked_runs_when_a_dependency_is_added()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var blocker = await AutomationTestData.CreateTask(scope.Db, scenario, "Blocking Task");
        var relationType = await AutomationTestData.CreateRelationType(
            scope.Db,
            scenario,
            RelationCategory.Dependency);

        await AutomationTestData.CreateRelation(scope.Db, scenario, relationType, blocker.Id, scenario.Task.Id);
        await AutomationTestData.CreateTaskStateRule(scope.Db, scenario, AutomationTriggerType.TaskBlocked);

        await scope.AutomationExecution.ExecuteEventRules(new TaskRelationChangedMessage
        {
            WorkspaceId = scenario.Workspace.Id,
            SourceTaskId = blocker.Id,
            TargetTaskId = scenario.Task.Id,
            Category = RelationCategory.Dependency,
            Change = TaskRelationChange.Added,
            ActorUserId = scenario.Owner.Id,
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.EntityId.Should().Be(scenario.Task.Id);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task TaskUnblocked_runs_when_the_last_blocker_completes()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var blocker = await AutomationTestData.CreateTask(scope.Db, scenario, "Blocking Task");
        var relationType = await AutomationTestData.CreateRelationType(
            scope.Db,
            scenario,
            RelationCategory.Dependency);
        var activeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");

        await AutomationTestData.CreateRelation(scope.Db, scenario, relationType, blocker.Id, scenario.Task.Id);
        await AutomationTestData.SetTaskStatus(scope.Db, blocker.Id, completeStatusId);
        await AutomationTestData.CreateTaskStateRule(scope.Db, scenario, AutomationTriggerType.TaskUnblocked);

        await scope.AutomationExecution.ExecuteEventRules(new TaskChangedMessage
        {
            TaskId = blocker.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, activeStatusId, completeStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.EntityId.Should().Be(scenario.Task.Id);
    }

    [Fact]
    public async Task TaskUnblocked_does_not_run_while_another_blocker_is_incomplete()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var firstBlocker = await AutomationTestData.CreateTask(scope.Db, scenario, "First Blocker");
        var secondBlocker = await AutomationTestData.CreateTask(scope.Db, scenario, "Second Blocker");
        var relationType = await AutomationTestData.CreateRelationType(
            scope.Db,
            scenario,
            RelationCategory.Dependency);
        var activeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");

        await AutomationTestData.CreateRelation(scope.Db, scenario, relationType, firstBlocker.Id, scenario.Task.Id);
        await AutomationTestData.CreateRelation(scope.Db, scenario, relationType, secondBlocker.Id, scenario.Task.Id);
        await AutomationTestData.SetTaskStatus(scope.Db, firstBlocker.Id, completeStatusId);
        await AutomationTestData.CreateTaskStateRule(scope.Db, scenario, AutomationTriggerType.TaskUnblocked);

        await scope.AutomationExecution.ExecuteEventRules(new TaskChangedMessage
        {
            TaskId = firstBlocker.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, activeStatusId, completeStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().BeEmpty();
    }

    [Fact]
    public async Task SubtasksCompleted_runs_for_the_parent_when_the_last_child_completes()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var child = await AutomationTestData.CreateTask(scope.Db, scenario, "Child Task");
        var relationType = await AutomationTestData.CreateRelationType(scope.Db, scenario);
        var activeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "in-progress");
        var completeStatusId = await AutomationTestData.GetStatusId(scope.Db, scenario, "complete");

        await AutomationTestData.CreateRelation(scope.Db, scenario, relationType, scenario.Task.Id, child.Id);
        await AutomationTestData.SetTaskStatus(scope.Db, child.Id, completeStatusId);
        await AutomationTestData.CreateTaskStateRule(scope.Db, scenario, AutomationTriggerType.SubtasksCompleted);

        await scope.AutomationExecution.ExecuteEventRules(new TaskChangedMessage
        {
            TaskId = child.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
            Changes =
            [
                TaskFieldChange.Create(TaskChangeField.Status, activeStatusId, completeStatusId),
            ],
        }, TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.EntityId.Should().Be(scenario.Task.Id);
    }

    [Fact]
    public async Task SprintEndingSoon_runs_for_sprints_inside_the_lead_time()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var sprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint One");

        await AutomationTestData.SetSprintEndDate(scope.Db, sprint.Id, DateTime.UtcNow.Date.AddDays(2));
        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, sprint.Id);
        await AutomationTestData.CreateTaskStateRule(
            scope.Db,
            scenario,
            AutomationTriggerType.SprintEndingSoon,
            durationDays: 3);

        await scope.AutomationExecution.ExecuteScheduledRules(
            AutomationTriggerType.SprintEndingSoon,
            TestContext.Current.CancellationToken);

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.EntityId.Should().Be(scenario.Task.Id);
    }

    [Fact]
    public async Task SprintEndingSoon_ignores_sprints_outside_the_lead_time()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var sprint = await AutomationTestData.CreateSprint(scope.Db, scenario, "Sprint One");

        await AutomationTestData.SetSprintEndDate(scope.Db, sprint.Id, DateTime.UtcNow.Date.AddDays(10));
        await AutomationTestData.AssignTaskToSprint(scope.Db, scenario.Task.Id, sprint.Id);
        await AutomationTestData.CreateTaskStateRule(
            scope.Db,
            scenario,
            AutomationTriggerType.SprintEndingSoon,
            durationDays: 3);

        await scope.AutomationExecution.ExecuteScheduledRules(
            AutomationTriggerType.SprintEndingSoon,
            TestContext.Current.CancellationToken);

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().BeEmpty();
    }
}
