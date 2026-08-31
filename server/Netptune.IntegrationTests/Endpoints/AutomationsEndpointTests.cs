using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Models.Automations;
using Netptune.Core.Requests;
using Netptune.Core.Requests.ServiceAccounts;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Automations;
using Netptune.Core.ViewModels.Projects;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.ServiceAccounts;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class AutomationsEndpointTests(NetptuneFixture fixture)
{
    private static readonly SemaphoreSlim SetupLock = new(1, 1);

    private static AutomationTestSetup? Setup;

    private readonly HttpClient Client = fixture.CreateNetptuneClient();

    private sealed record AutomationTestSetup(int ProjectId, string ExecutionUserId);

    [Fact]
    public async Task DryRun_ShouldReportMatch_WhenConditionMatchesTask()
    {
        var task = await CreateTask("Automation dry run match");
        var rule = await CreateRule(NameContains("dry run match"));

        var dryRun = await GetDryRun(rule.Id, task.Id);

        dryRun.ConditionsMatch.Should().BeTrue();
        dryRun.TaskName.Should().Be(task.Name);
        dryRun.HasUnevaluableConditions.Should().BeFalse();
        dryRun.ConditionGroup.Should().NotBeNull();
        dryRun.ConditionGroup!.Conditions.Should().ContainSingle();
        dryRun.ConditionGroup.Conditions[0].IsMatch.Should().BeTrue();
    }

    [Fact]
    public async Task DryRun_ShouldReportActualValue_WhenConditionFails()
    {
        var task = await CreateTask("Automation dry run miss");
        var rule = await CreateRule(NameContains("a name the task does not have"));

        var dryRun = await GetDryRun(rule.Id, task.Id);

        dryRun.ConditionsMatch.Should().BeFalse();
        dryRun.ConditionGroup!.Conditions[0].IsMatch.Should().BeFalse();
        dryRun.ConditionGroup.Conditions[0].ActualValue.Should().Be(task.Name);
    }

    [Fact]
    public async Task DryRun_ShouldMarkChangeOperatorsUnevaluable()
    {
        var task = await CreateTask("Automation dry run change operator");
        var conditionGroup = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions =
            [
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Name,
                    Operator = AutomationConditionOperator.Any,
                },
            ],
        };
        var rule = await CreateRule(conditionGroup);

        var dryRun = await GetDryRun(rule.Id, task.Id);

        dryRun.HasUnevaluableConditions.Should().BeTrue();
        dryRun.ConditionGroup!.Conditions[0].IsEvaluable.Should().BeFalse();
    }

    [Fact]
    public async Task DryRun_ShouldMarkEventTriggersUnevaluable()
    {
        var task = await CreateTask("Automation dry run event trigger");
        var rule = await CreateRule(NameContains("event trigger"));

        var dryRun = await GetDryRun(rule.Id, task.Id);

        dryRun.TriggerIsEvaluable.Should().BeFalse();
        dryRun.TriggerMatches.Should().BeFalse();
    }

    [Fact]
    public async Task DryRun_ShouldReportTriggerState_ForScheduledTriggers()
    {
        var task = await CreateTask("Automation dry run overdue trigger");
        var rule = await CreateOverdueRule();

        var dryRun = await GetDryRun(rule.Id, task.Id);

        dryRun.TriggerIsEvaluable.Should().BeTrue();
        dryRun.TriggerMatches.Should().BeFalse();
    }

    [Fact]
    public async Task DryRun_ShouldDescribeProposedEffects()
    {
        var task = await CreateTask("Automation dry run effects");
        var rule = await CreateRule(NameContains("dry run effects"));

        var dryRun = await GetDryRun(rule.Id, task.Id);

        dryRun.Actions.Should().ContainSingle();

        var action = dryRun.Actions[0];

        action.Type.Should().Be(AutomationActionType.NotifyTaskAssignees);
        action.HasEffect.Should().BeTrue();
        action.RecipientRoles.Should().Contain(WorkspaceRole.Admin);
        action.Message.Should().Be($"Check {task.Name}");
    }

    [Fact]
    public async Task DryRun_ShouldNotChangeTheTask()
    {
        var task = await CreateTask("Automation dry run is read only");
        var rule = await CreateRule(NameContains("read only"));

        await GetDryRun(rule.Id, task.Id);

        var reloaded = await Client.GetFromJsonAsync<TaskViewModel>($"api/tasks/{task.Id}");

        reloaded.Should().NotBeNull();
        reloaded.Name.Should().Be(task.Name);
        reloaded.StatusId.Should().Be(task.StatusId);
    }

    [Fact]
    public async Task DryRun_ShouldReturnNotFound_ForTaskInAnotherWorkspace()
    {
        var task = await CreateTask("Automation dry run isolation");
        var rule = await CreateRule(NameContains("isolation"));

        (await Client.GetAsync($"api/automations/{rule.Id}/dry-run/{task.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var foreignTaskId = await GetTaskIdInWorkspace("linux");

        var response = await Client.GetAsync($"api/automations/{rule.Id}/dry-run/{foreignTaskId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Run_ShouldQueueRule_ForSelectedTask()
    {
        var task = await CreateTask("Automation manual run");
        var rule = await CreateRule(NameContains("manual run"));

        var response = await Client.PostAsJsonAsync(
            $"api/automations/{rule.Id}/run",
            new AutomationManualRunRequestBody { TaskIds = [task.Id] });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationManualRunViewModel>>();

        result.Payload!.RuleId.Should().Be(rule.Id);
        result.Payload.TaskCount.Should().Be(1);
    }

    [Fact]
    public async Task Run_ShouldFail_ForTaskInAnotherWorkspace()
    {
        var rule = await CreateRule(NameContains("manual run isolation"));
        var foreignTaskId = await GetTaskIdInWorkspace("linux");

        var response = await Client.PostAsJsonAsync(
            $"api/automations/{rule.Id}/run",
            new AutomationManualRunRequestBody { TaskIds = [foreignTaskId] });

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationManualRunViewModel>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Run_ShouldFail_WithoutTasks()
    {
        var rule = await CreateRule(NameContains("manual run validation"));

        var response = await Client.PostAsJsonAsync(
            $"api/automations/{rule.Id}/run",
            new AutomationManualRunRequestBody());

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationManualRunViewModel>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Run_ShouldReturnNotFound_ForUnknownRule()
    {
        var task = await CreateTask("Automation manual run missing rule");

        var response = await Client.PostAsJsonAsync(
            "api/automations/999999/run",
            new AutomationManualRunRequestBody { TaskIds = [task.Id] });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetPaged_ShouldReturnRulesWithPaging()
    {
        var rule = await CreateRule(NameContains("paged listing"));

        var response = await Client.GetAsync("api/automations?page=1&pageSize=1");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>>();
        var page = result.Payload!;

        page.Page.Should().Be(1);
        page.PageSize.Should().Be(1);
        page.Items.Should().ContainSingle();
        page.TotalCount.Should().BeGreaterThanOrEqualTo(1);
        rule.Id.Should().BePositive();
    }

    [Fact]
    public async Task GetPaged_ShouldFilterBySearch()
    {
        var uniqueName = $"Searchable rule {Guid.NewGuid():N}";
        var rule = await CreateNamedRule(uniqueName);

        var response = await Client.GetAsync($"api/automations?search={Uri.EscapeDataString(uniqueName)}");

        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>>();
        var page = result.Payload!;

        page.Items.Should().ContainSingle();
        page.Items[0].Id.Should().Be(rule.Id);
        page.Items[0].Name.Should().Be(uniqueName);
    }

    [Fact]
    public async Task GetPaged_ShouldFilterByTriggerTypes()
    {
        var uniqueName = $"Trigger filtered rule {Guid.NewGuid():N}";
        var rule = await CreateNamedRule(uniqueName);

        var matching = await Client.GetAsync(
            $"api/automations?search={Uri.EscapeDataString(uniqueName)}&triggerTypes={(int)AutomationTriggerType.TaskChanged}");
        var matchingResult = await matching.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>>();

        matchingResult.Payload!.Items.Should().ContainSingle(item => item.Id == rule.Id);

        var excluded = await Client.GetAsync(
            $"api/automations?search={Uri.EscapeDataString(uniqueName)}&triggerTypes={(int)AutomationTriggerType.TaskOverdue}");
        var excludedResult = await excluded.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>>();

        excludedResult.Payload!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummary_ShouldCountRules()
    {
        await CreateRule(NameContains("summary counting"));

        var response = await Client.GetAsync("api/automations/summary");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<AutomationRuleSummaryViewModel>>();
        var summary = result.Payload!;

        summary.RuleCount.Should().BeGreaterThanOrEqualTo(1);
        summary.EnabledCount.Should().BeGreaterThanOrEqualTo(1);
        summary.RecentFailureCount.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public async Task Create_ShouldPersistProjectScope()
    {
        var setup = await GetSetup();
        var response = await Client.PostAsJsonAsync("api/automations", new AutomationRuleRequest
        {
            Name = $"Scoped rule {Guid.NewGuid():N}",
            IsEnabled = true,
            ExecutionUserId = setup.ExecutionUserId,
            ProjectId = setup.ProjectId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Status],
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Recipients = [AutomationNotificationRecipient.Assignees],
                },
            ],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();
        var created = result.Payload!;

        created.ProjectId.Should().Be(setup.ProjectId);
        created.BoardId.Should().BeNull();
        created.SprintId.Should().BeNull();

        var reloaded = await Client.GetFromJsonAsync<ClientResponse<AutomationRuleViewModel>>(
            $"api/automations/{created.Id}");

        reloaded.Payload!.ProjectId.Should().Be(setup.ProjectId);
    }

    [Fact]
    public async Task Create_ShouldFail_WithMoreThanOneScope()
    {
        var setup = await GetSetup();
        var response = await Client.PostAsJsonAsync("api/automations", new AutomationRuleRequest
        {
            Name = $"Over scoped rule {Guid.NewGuid():N}",
            IsEnabled = true,
            ExecutionUserId = setup.ExecutionUserId,
            ProjectId = setup.ProjectId,
            SprintId = 1,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Status],
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Recipients = [AutomationNotificationRecipient.Assignees],
                },
            ],
        });

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Clone_ShouldCopyRule_AsDisabledDraft()
    {
        var rule = await CreateRule(NameContains("clone source"));

        var response = await Client.PostAsJsonAsync(
            $"api/automations/{rule.Id}/clone",
            new AutomationCloneRequest());

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();
        var clone = result.Payload!;

        clone.Id.Should().NotBe(rule.Id);
        clone.Name.Should().Be($"{rule.Name} (copy)");
        clone.IsEnabled.Should().BeFalse();
        clone.ExecutionUserId.Should().Be(rule.ExecutionUserId);
        clone.Trigger.Type.Should().Be(rule.Trigger.Type);
        clone.Trigger.ConditionGroup.Should().BeEquivalentTo(rule.Trigger.ConditionGroup);
        clone.Actions.Should().HaveCount(rule.Actions.Count);
        clone.Actions[0].Type.Should().Be(rule.Actions[0].Type);
    }

    [Fact]
    public async Task Clone_ShouldUseTheRequestedName()
    {
        var rule = await CreateRule(NameContains("clone naming"));

        var response = await Client.PostAsJsonAsync(
            $"api/automations/{rule.Id}/clone",
            new AutomationCloneRequest { Name = "Renamed clone" });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();

        result.Payload!.Name.Should().Be("Renamed clone");
        result.Payload.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public async Task Clone_ShouldReturnNotFound_ForUnknownRule()
    {
        var response = await Client.PostAsJsonAsync(
            "api/automations/999999/clone",
            new AutomationCloneRequest());

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<AutomationRuleViewModel> CreateNamedRule(string name)
    {
        var setup = await GetSetup();
        var response = await Client.PostAsJsonAsync("api/automations", new AutomationRuleRequest
        {
            Name = name,
            IsEnabled = true,
            ExecutionUserId = setup.ExecutionUserId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Status],
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Recipients = [AutomationNotificationRecipient.Assignees],
                },
            ],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();

        return result.Payload!;
    }

    private async Task<int> GetTaskIdInWorkspace(string slug)
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.ProjectTasks
            .Where(task => task.Workspace!.Slug == slug && !task.IsDeleted)
            .Select(task => task.Id)
            .FirstAsync();
    }

    [Fact]
    public async Task GetRule_ShouldReportNoWarnings_WhenReferencesResolve()
    {
        var rule = await CreateRule(NameContains("healthy rule"));

        var fetched = await GetRule(rule.Id);

        fetched.Warnings.Should().BeEmpty();
    }

    [Fact]
    public async Task GetRule_ShouldWarn_WhenActionStatusWasDeleted()
    {
        var statusId = await CreateStatus($"Automation warning {Guid.NewGuid():N}");
        var rule = await CreateStatusRule(statusId);

        await DeleteStatus(statusId);

        var fetched = await GetRule(rule.Id);

        fetched.Warnings.Should().ContainSingle(warning => warning.Code == AutomationWarningCode.MissingStatus);
        fetched.Warnings[0].ActionId.Should().Be(fetched.Actions[0].Id);
    }

    [Fact]
    public async Task GetRule_ShouldWarn_WhenConditionChecksDeletedStatus()
    {
        var statusId = await CreateStatus($"Automation condition warning {Guid.NewGuid():N}");
        var conditionGroup = new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions =
            [
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Status,
                    Operator = AutomationConditionOperator.Equals,
                    Value = statusId.ToString(),
                },
            ],
        };

        var rule = await CreateRule(conditionGroup);

        await DeleteStatus(statusId);

        var fetched = await GetRule(rule.Id);

        fetched.Warnings.Should().ContainSingle(warning => warning.Code == AutomationWarningCode.MissingStatus);
        fetched.Warnings[0].ActionId.Should().BeNull();
    }

    [Fact]
    public async Task GetRules_ShouldIncludeWarnings_ForListedRules()
    {
        var statusId = await CreateStatus($"Automation list warning {Guid.NewGuid():N}");
        var rule = await CreateStatusRule(statusId);

        await DeleteStatus(statusId);

        var response = await Client.GetAsync($"api/automations?search={Uri.EscapeDataString(rule.Name)}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content
            .ReadFromJsonAsync<ClientResponse<PagedResponse<AutomationRuleListItemViewModel>>>();
        var listed = result.Payload!.Items.Single(item => item.Id == rule.Id);

        listed.Warnings.Should().ContainSingle(warning => warning.Code == AutomationWarningCode.MissingStatus);
    }

    [Fact]
    public async Task Update_ShouldReplaceTheRule_WhenInputValid()
    {
        var setup = await GetSetup();
        var rule = await CreateRule(NameContains("update source"));
        var name = $"Updated rule {Guid.NewGuid():N}";

        var response = await Client.PutAsJsonAsync($"api/automations/{rule.Id}", new AutomationRuleRequest
        {
            Name = name,
            IsEnabled = false,
            ExecutionUserId = setup.ExecutionUserId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskCreated,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Recipients = [AutomationNotificationRecipient.Assignees],
                },
            ],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.Name.Should().Be(name);
        result.Payload.IsEnabled.Should().BeFalse();
        result.Payload.Trigger.Type.Should().Be(AutomationTriggerType.TaskCreated);

        (await GetRule(rule.Id)).Name.Should().Be(name);
    }

    [Fact]
    public async Task Update_ShouldReturnNotFound_WhenRuleDoesNotExist()
    {
        var setup = await GetSetup();

        var response = await Client.PutAsJsonAsync("api/automations/999999", new AutomationRuleRequest
        {
            Name = "Missing rule",
            IsEnabled = true,
            ExecutionUserId = setup.ExecutionUserId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskCreated,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Recipients = [AutomationNotificationRecipient.Assignees],
                },
            ],
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetRuns_ShouldReturnTheRunLog_ForTheRule()
    {
        var task = await CreateTask("Automation run log");
        var rule = await CreateRule(NameContains("run log"));

        // A manual run only dispatches a message; the run row is written by the automation worker,
        // which this host does not run. Seed the row the worker would have written instead.
        var run = await SeedRun(rule.Id, task.Id);

        var response = await Client.GetAsync($"api/automations/{rule.Id}/runs");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<PagedResponse<AutomationRunViewModel>>>();

        result.IsSuccess.Should().BeTrue();
        result.Payload!.TotalCount.Should().Be(1);
        result.Payload!.Items.Should().ContainSingle(item => item.Id == run);
    }

    [Fact]
    public async Task GetRuns_ShouldReturnNotFound_WhenRuleDoesNotExist()
    {
        var response = await Client.GetAsync("api/automations/999999/runs");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DisableThenEnable_ShouldToggleTheRule()
    {
        var rule = await CreateRule(NameContains("toggle source"));

        rule.IsEnabled.Should().BeTrue();

        var disable = await Client.PostAsync($"api/automations/{rule.Id}/disable", null);

        disable.StatusCode.Should().Be(HttpStatusCode.OK, await disable.Content.ReadAsStringAsync());
        (await GetRule(rule.Id)).IsEnabled.Should().BeFalse();

        var enable = await Client.PostAsync($"api/automations/{rule.Id}/enable", null);

        enable.StatusCode.Should().Be(HttpStatusCode.OK, await enable.Content.ReadAsStringAsync());
        (await GetRule(rule.Id)).IsEnabled.Should().BeTrue();
    }

    [Fact]
    public async Task Enable_ShouldReturnNotFound_WhenRuleDoesNotExist()
    {
        var response = await Client.PostAsync("api/automations/999999/enable", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Disable_ShouldReturnNotFound_WhenRuleDoesNotExist()
    {
        var response = await Client.PostAsync("api/automations/999999/disable", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheRule_WhenInputValid()
    {
        var rule = await CreateRule(NameContains("delete source"));

        var response = await Client.DeleteAsync($"api/automations/{rule.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse>();

        result.IsSuccess.Should().BeTrue();

        (await Client.GetAsync($"api/automations/{rule.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenRuleDoesNotExist()
    {
        var response = await Client.DeleteAsync("api/automations/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<int> SeedRun(int ruleId, int taskId)
    {
        using var scope = fixture.CreateScope();

        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var run = new AutomationRun
        {
            AutomationRuleId = ruleId,
            EntityId = taskId,
            EntityType = EntityType.Task,
            TriggerType = AutomationTriggerType.TaskChanged,
            Status = AutomationRunStatus.Succeeded,
            IdempotencyKey = Guid.NewGuid().ToString("N"),
        };

        context.Add(run);

        await context.SaveChangesAsync();

        return run.Id;
    }

    private async Task<AutomationRuleViewModel> GetRule(int ruleId)
    {
        var response = await Client.GetAsync($"api/automations/{ruleId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();

        return result.Payload!;
    }

    private async Task<int> CreateStatus(string name)
    {
        var response = await Client.PostAsJsonAsync("api/statuses", new CreateStatusRequest
        {
            EntityType = EntityType.Task,
            Name = name,
            Category = StatusCategory.Backlog,
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<StatusViewModel>>();

        return result.Payload!.Id;
    }

    private async Task DeleteStatus(int statusId)
    {
        var response = await Client.DeleteAsync($"api/statuses/{statusId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
    }

    private async Task<AutomationRuleViewModel> CreateStatusRule(int statusId)
    {
        var setup = await GetSetup();
        var response = await Client.PostAsJsonAsync("api/automations", new AutomationRuleRequest
        {
            Name = $"Warning rule {Guid.NewGuid():N}",
            IsEnabled = true,
            ExecutionUserId = setup.ExecutionUserId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskCreated,
            },
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.UpdateTask,
                    StatusId = statusId,
                },
            ],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();

        return result.Payload!;
    }

    private static AutomationConditionGroup NameContains(string value)
    {
        return new AutomationConditionGroup
        {
            Operator = AutomationConditionGroupOperator.All,
            Conditions =
            [
                new AutomationFieldCondition
                {
                    Field = TaskChangeField.Name,
                    Operator = AutomationConditionOperator.Contains,
                    Value = value,
                },
            ],
        };
    }

    private async Task<AutomationDryRunViewModel> GetDryRun(int ruleId, int taskId)
    {
        var response = await Client.GetAsync($"api/automations/{ruleId}/dry-run/{taskId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationDryRunViewModel>>();

        return result.Payload!;
    }

    private async Task<AutomationTestSetup> GetSetup()
    {
        if (Setup is not null)
        {
            return Setup;
        }

        await SetupLock.WaitAsync();

        try
        {
            Setup ??= new AutomationTestSetup(
                await CreateProjectId(),
                await CreateServiceAccountUserId());

            return Setup;
        }
        finally
        {
            SetupLock.Release();
        }
    }

    private Task<AutomationRuleViewModel> CreateOverdueRule()
    {
        return CreateRule(new AutomationTriggerRequest
        {
            Type = AutomationTriggerType.TaskOverdue,
        });
    }

    private Task<AutomationRuleViewModel> CreateRule(AutomationConditionGroup conditionGroup)
    {
        return CreateRule(new AutomationTriggerRequest
        {
            Type = AutomationTriggerType.TaskChanged,
            Fields = [TaskChangeField.Name],
            ConditionGroup = conditionGroup,
        });
    }

    private async Task<AutomationRuleViewModel> CreateRule(AutomationTriggerRequest trigger)
    {
        var setup = await GetSetup();
        var response = await Client.PostAsJsonAsync("api/automations", new AutomationRuleRequest
        {
            Name = $"Dry run rule {Guid.NewGuid():N}",
            IsEnabled = true,
            ExecutionUserId = setup.ExecutionUserId,
            Trigger = trigger,
            Actions =
            [
                new AutomationActionRequest
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    Message = "Check {{task.name}}",
                    Recipients = [AutomationNotificationRecipient.WorkspaceRoles],
                    RecipientRoles = [WorkspaceRole.Admin],
                },
            ],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<AutomationRuleViewModel>>();

        return result.Payload!;
    }

    private async Task<string> CreateServiceAccountUserId()
    {
        var response = await Client.PostAsJsonAsync("api/service-accounts", new CreateServiceAccountRequest
        {
            Name = $"Dry run agent {Guid.NewGuid():N}",
            Description = "Created by the automation dry run integration test.",
            Permissions = [NetptunePermissions.Tasks.Read, NetptunePermissions.Tasks.Update],
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        var account = await response.Content.ReadFromJsonAsync<ServiceAccountViewModel>();

        return account!.UserId;
    }

    private async Task<int> CreateProjectId()
    {
        var response = await Client.PostAsJsonAsync("api/projects", new AddProjectRequest
        {
            // Lead with the guid so the derived project key stays unique across test projects.
            Name = $"{Guid.NewGuid():N} Automation"[..30],
            Description = "Automation integration test project",
            MetaInfo = new ProjectMeta { Color = "blue" },
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<ProjectViewModel>>();

        return result.Payload!.Id;
    }

    private async Task<TaskViewModel> CreateTask(string name)
    {
        var setup = await GetSetup();
        var response = await Client.PostAsJsonAsync("api/tasks", new AddProjectTaskRequest
        {
            Name = name,
            Description = "Automation integration test task",
            ProjectId = setup.ProjectId,
        });

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<TaskViewModel>>();

        return result.Payload!;
    }
}
