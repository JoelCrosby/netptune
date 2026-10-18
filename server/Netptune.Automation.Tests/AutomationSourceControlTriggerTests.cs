using FluentAssertions;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Events.SourceControl;
using Netptune.Core.Meta;

using Xunit;

namespace Netptune.Automation.Tests;

[Collection("automation-database")]
public sealed class AutomationSourceControlTriggerTests
{
    private readonly AutomationTestFixture Fixture;

    public AutomationSourceControlTriggerTests(AutomationTestFixture fixture)
    {
        Fixture = fixture;
    }

    [Fact]
    public async Task PullRequestOpened_runs_for_a_matching_target_branch()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateSourceControlRule(
            scope.Db,
            scenario,
            AutomationTriggerType.PullRequestOpened,
            targetBranches: ["release/*"]);

        await Execute(scope, PullRequestEvent(scenario, AutomationTriggerType.PullRequestOpened, "release/2.0"));

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.EntityId.Should().Be(scenario.Task.Id);
        run.Status.Should().Be(AutomationRunStatus.Succeeded);
    }

    [Fact]
    public async Task PullRequestOpened_ignores_other_target_branches()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateSourceControlRule(
            scope.Db,
            scenario,
            AutomationTriggerType.PullRequestOpened,
            targetBranches: ["main"]);

        await Execute(scope, PullRequestEvent(scenario, AutomationTriggerType.PullRequestOpened, "develop"));

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().BeEmpty();
    }

    [Fact]
    public async Task PullRequestOpened_waits_for_a_draft_to_be_ready_unless_drafts_are_included()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var excludingRule = await AutomationTestData.CreateSourceControlRule(
            scope.Db,
            scenario,
            AutomationTriggerType.PullRequestOpened);
        var includingRule = await AutomationTestData.CreateSourceControlRule(
            scope.Db,
            scenario,
            AutomationTriggerType.PullRequestOpened,
            includeDrafts: true);

        await Execute(scope, PullRequestEvent(scenario, AutomationTriggerType.PullRequestOpened, "main") with
        {
            IsDraft = true,
        });
        await Execute(scope, PullRequestEvent(scenario, AutomationTriggerType.PullRequestOpened, "main") with
        {
            IsReadyForReview = true,
        });

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);
        var ruleIds = runs.Select(run => run.AutomationRuleId).ToList();

        ruleIds.Should().BeEquivalentTo([includingRule.Id, excludingRule.Id]);
    }

    [Fact]
    public async Task PullRequestMerged_runs_once_when_the_webhook_is_redelivered()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);

        await AutomationTestData.CreateSourceControlRule(scope.Db, scenario, AutomationTriggerType.PullRequestMerged);

        var message = PullRequestEvent(scenario, AutomationTriggerType.PullRequestMerged, "main");

        await Execute(scope, message);
        await Execute(scope, message);

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().ContainSingle();
    }

    [Fact]
    public async Task ChecksFailed_ignores_pull_request_rules()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var checksRule = await AutomationTestData.CreateSourceControlRule(
            scope.Db,
            scenario,
            AutomationTriggerType.ChecksFailed);

        await AutomationTestData.CreateSourceControlRule(scope.Db, scenario, AutomationTriggerType.PullRequestMerged);

        await Execute(scope, PullRequestEvent(scenario, AutomationTriggerType.ChecksFailed, null));

        var run = await scope.Db.AutomationRuns.SingleAsync(TestContext.Current.CancellationToken);

        run.AutomationRuleId.Should().Be(checksRule.Id);
    }

    [Fact]
    public async Task SourceControl_triggers_ignore_tasks_from_another_workspace()
    {
        await using var scope = await Fixture.CreateScope();

        var scenario = await AutomationTestData.CreateScenario(scope.Db);
        var otherWorkspace = new Workspace
        {
            Name = "Other Workspace",
            Slug = "other-workspace",
            MetaInfo = new WorkspaceMeta(),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        scope.Db.Workspaces.Add(otherWorkspace);

        await scope.Db.SaveChangesAsync(TestContext.Current.CancellationToken);
        await scope.Db.ProjectTasks
            .Where(task => task.Id == scenario.Task.Id)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(task => task.WorkspaceId, otherWorkspace.Id),
                TestContext.Current.CancellationToken);
        await AutomationTestData.CreateSourceControlRule(scope.Db, scenario, AutomationTriggerType.PullRequestMerged);

        await Execute(scope, PullRequestEvent(scenario, AutomationTriggerType.PullRequestMerged, "main"));

        var runs = await scope.Db.AutomationRuns.ToListAsync(TestContext.Current.CancellationToken);

        runs.Should().BeEmpty();
    }

    private static SourceControlTaskEventMessage PullRequestEvent(
        AutomationScenario scenario,
        AutomationTriggerType triggerType,
        string? targetBranch)
    {
        return new SourceControlTaskEventMessage
        {
            EventId = Guid.NewGuid(),
            WorkspaceId = scenario.Workspace.Id,
            TriggerType = triggerType,
            TaskIds = [scenario.Task.Id],
            TargetBranch = targetBranch,
            OccurredAt = DateTime.UtcNow,
        };
    }

    private static async Task Execute(AutomationTestScope scope, SourceControlTaskEventMessage message)
    {
        await scope.AutomationExecution.ExecuteEventRules(message, TestContext.Current.CancellationToken);
    }
}
