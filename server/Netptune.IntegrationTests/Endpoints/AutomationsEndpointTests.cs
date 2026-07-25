using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Authorization;
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
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

public sealed class AutomationsEndpointTests(NetptuneFixture fixture)
{
    // The project and service account never vary between cases, so build them once.
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
        reloaded!.Name.Should().Be(task.Name);
        reloaded.StatusId.Should().Be(task.StatusId);
    }

    [Fact]
    public async Task DryRun_ShouldReturnNotFound_ForTaskInAnotherWorkspace()
    {
        var task = await CreateTask("Automation dry run isolation");
        var rule = await CreateRule(NameContains("isolation"));

        // The rule can read its own workspace's task, proving the 404 below is isolation
        // and not a broken rule.
        (await Client.GetAsync($"api/automations/{rule.Id}/dry-run/{task.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var foreignTaskId = await GetTaskIdInWorkspace("linux");

        var response = await Client.GetAsync($"api/automations/{rule.Id}/dry-run/{foreignTaskId}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<int> GetTaskIdInWorkspace(string slug)
    {
        using var scope = fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await context.ProjectTasks
            .Where(task => task.Workspace.Slug == slug && !task.IsDeleted)
            .Select(task => task.Id)
            .FirstAsync();
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

        return result!.Payload!;
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

    private async Task<AutomationRuleViewModel> CreateRule(AutomationConditionGroup conditionGroup)
    {
        var setup = await GetSetup();
        var response = await Client.PostAsJsonAsync("api/automations", new AutomationRuleRequest
        {
            Name = $"Dry run rule {Guid.NewGuid():N}",
            IsEnabled = true,
            ExecutionUserId = setup.ExecutionUserId,
            Trigger = new AutomationTriggerRequest
            {
                Type = AutomationTriggerType.TaskChanged,
                Fields = [TaskChangeField.Name],
                ConditionGroup = conditionGroup,
            },
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

        return result!.Payload!;
    }

    private async Task<string> CreateServiceAccountUserId()
    {
        var response = await Client.PostAsJsonAsync("api/service-accounts", new CreateServiceAccountRequest
        {
            Name = $"Dry run agent {Guid.NewGuid():N}",
            Description = "Created by the automation dry run integration test.",
            Permissions = [NetptunePermissions.Tasks.Read],
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

        return result!.Payload!.Id;
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

        return result!.Payload!;
    }
}
