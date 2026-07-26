using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;

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
    }
}
