using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Models.Activity;

using Xunit;

namespace Netptune.Automation.Tests;

[Collection("automation-database")]
public sealed class AutomationLimitGuardTests
{
    private readonly AutomationTestFixture Fixture;

    public AutomationLimitGuardTests(AutomationTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task Rule_is_disabled_after_repeated_failures()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskStateRule(
            scope.Db,
            scenario,
            AutomationTriggerType.TaskCreated);

        await AutomationTestData.CreateRuns(scope.Db, rule, 20, AutomationRunStatus.Failed);

        await scope.AutomationExecution.ExecuteEventRules(new TaskCreatedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
        }, TestContext.Current.CancellationToken);

        var disabledRule = await scope.Db.AutomationRules
            .SingleAsync(candidate => candidate.Id == rule.Id, TestContext.Current.CancellationToken);
        var runCount = await scope.Db.AutomationRuns
            .CountAsync(run => run.AutomationRuleId == rule.Id, TestContext.Current.CancellationToken);

        disabledRule.IsEnabled.Should().BeFalse();
        disabledRule.AutoDisabledAt.Should().NotBeNull();
        disabledRule.AutoDisabledReason.Should().Contain("failed runs");
        disabledRule.AutoDisabledReasonCode.Should().Be("disabledAfterFailures");
        disabledRule.AutoDisabledReasonArguments.Should().HaveCount(2);
        runCount.Should().Be(20);
    }

    [Fact]
    public async Task Rule_keeps_running_below_the_failure_threshold()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskStateRule(
            scope.Db,
            scenario,
            AutomationTriggerType.TaskCreated);

        await AutomationTestData.CreateRuns(scope.Db, rule, 5, AutomationRunStatus.Failed);

        await scope.AutomationExecution.ExecuteEventRules(new TaskCreatedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
        }, TestContext.Current.CancellationToken);

        var activeRule = await scope.Db.AutomationRules
            .SingleAsync(candidate => candidate.Id == rule.Id, TestContext.Current.CancellationToken);
        var runCount = await scope.Db.AutomationRuns
            .CountAsync(run => run.AutomationRuleId == rule.Id, TestContext.Current.CancellationToken);

        activeRule.IsEnabled.Should().BeTrue();
        activeRule.AutoDisabledAt.Should().BeNull();
        runCount.Should().Be(6);
    }

    [Fact]
    public async Task Rule_is_disabled_after_an_excessive_number_of_runs()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskStateRule(
            scope.Db,
            scenario,
            AutomationTriggerType.TaskCreated);

        await AutomationTestData.CreateRuns(scope.Db, rule, 500, AutomationRunStatus.Succeeded);

        await scope.AutomationExecution.ExecuteEventRules(new TaskCreatedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
        }, TestContext.Current.CancellationToken);

        var disabledRule = await scope.Db.AutomationRules
            .SingleAsync(candidate => candidate.Id == rule.Id, TestContext.Current.CancellationToken);

        disabledRule.IsEnabled.Should().BeFalse();
        disabledRule.AutoDisabledReason.Should().Contain("runs within");
        disabledRule.AutoDisabledReasonCode.Should().Be("disabledAfterRuns");
    }

    [Fact]
    public async Task Auto_disabling_a_rule_records_an_event_and_cancels_its_delayed_actions()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var rule = await AutomationTestData.CreateTaskStateRule(
            scope.Db,
            scenario,
            AutomationTriggerType.TaskCreated);
        var actionId = await scope.Db.Set<AutomationAction>()
            .Where(action => action.AutomationRuleId == rule.Id)
            .Select(action => action.Id)
            .FirstAsync(TestContext.Current.CancellationToken);
        var scheduled = new ScheduledAutomationAction
        {
            AutomationRuleId = rule.Id,
            AutomationActionId = actionId,
            TaskId = scenario.Task.Id,
            ActionType = AutomationActionType.NotifyTaskAssignees,
            Status = ScheduledAutomationActionStatus.Pending,
            ExpectedStatusId = scenario.Task.StatusId,
            ExecuteAt = DateTime.UtcNow.AddDays(1),
            IdempotencyKey = $"test:{Guid.NewGuid():N}",
            OwnerId = rule.ExecutionUserId,
        };

        scope.Db.Add(scheduled);

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await AutomationTestData.CreateRuns(scope.Db, rule, 20, AutomationRunStatus.Failed);

        await scope.AutomationExecution.ExecuteEventRules(new TaskCreatedMessage
        {
            TaskId = scenario.Task.Id,
            WorkspaceId = scenario.Workspace.Id,
            ActorUserId = scenario.Owner.Id,
        }, TestContext.Current.CancellationToken);

        var subjectId = rule.Id.ToString();
        var disabledEvent = scope.EventRecords.Events.Single(record =>
            record.EventKey == EventKeys.AutomationRuleAutoDisabled &&
            record.SubjectId == subjectId);
        var disabledPayload = (AutomationRuleActivityMeta)disabledEvent.Payload;
        var scheduledStatus = await scope.Db.Set<ScheduledAutomationAction>()
            .AsNoTracking()
            .Where(action => action.Id == scheduled.Id)
            .Select(action => action.Status)
            .SingleAsync(TestContext.Current.CancellationToken);

        disabledEvent.SubjectType.Should().Be(EventEntityTypes.From(EntityType.AutomationRule));
        disabledEvent.ActorUserId.Should().Be(rule.ExecutionUserId);
        disabledEvent.OriginType.Should().Be(EventOriginType.System);
        disabledPayload.Reason.Should().Contain("failed runs");
        disabledPayload.CancelledScheduledActions.Should().Be(1);
        scheduledStatus.Should().Be(ScheduledAutomationActionStatus.Cancelled);
    }
}
