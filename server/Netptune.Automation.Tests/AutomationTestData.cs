using System.Text.Json;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Models.Automations;
using Netptune.Core.Relationships;
using Netptune.Core.Statuses;
using Netptune.Entities.Contexts;

namespace Netptune.Automation.Tests;

internal static class AutomationTestData
{
    public const string OwnerUserId = "owner-user";
    public const string AssigneeUserId = "assignee-user";
    public const string ExecutionUserId = "automation-service-user";

    public static async Task<AutomationScenario> CreateScenario(
        DataContext db,
        string taskStatusKey = "new",
        bool assignTask = true,
        DateTime? taskUpdatedAt = null,
        DateOnly? dueDate = null)
    {
        var owner = CreateUser(OwnerUserId, "owner@example.test");
        var assignee = CreateUser(AssigneeUserId, "assignee@example.test");
        var executionUser = CreateUser(ExecutionUserId, "automation@example.test");
        executionUser.UserType = AppUserType.ServiceAccount;
        var workspace = new Workspace
        {
            Name = "Automation Workspace",
            Slug = "automation-workspace",
            MetaInfo = new WorkspaceMeta(),
            OwnerId = owner.Id,
            CreatedByUserId = owner.Id,
        };

        var statuses = DefaultTaskStatuses.All
            .Select(definition =>
            {
                var status = DefaultTaskStatuses.Create(definition, 0, owner.Id);
                status.Workspace = workspace;
                return status;
            })
            .ToList();
        var taskStatusEntity = statuses.Single(status => status.Key == taskStatusKey);

        var project = new Project
        {
            Name = "Automation Project",
            Key = "AUTO",
            Workspace = workspace,
            MetaInfo = new ProjectMeta(),
            OwnerId = owner.Id,
            CreatedByUserId = owner.Id,
        };

        var task = new ProjectTask
        {
            Name = "Automation Task",
            Status = taskStatusEntity,
            ProjectScopeId = 1,
            Project = project,
            Workspace = workspace,
            OwnerId = owner.Id,
            CreatedByUserId = owner.Id,
            UpdatedAt = taskUpdatedAt,
            DueDate = dueDate,
        };

        if (assignTask)
        {
            task.ProjectTaskAppUsers.Add(new ProjectTaskAppUser
            {
                UserId = assignee.Id,
            });
        }

        var serviceAccount = new ServiceAccount
        {
            UserId = executionUser.Id,
            Workspace = workspace,
            CreatedByUserId = owner.Id,
            CreatedAt = DateTime.UtcNow,
        };
        var serviceMembership = new WorkspaceAppUser
        {
            UserId = executionUser.Id,
            Workspace = workspace,
            Role = WorkspaceRole.Member,
            Permissions =
            [
                NetptunePermissions.Tasks.Read,
                NetptunePermissions.Tasks.Update,
                NetptunePermissions.Tasks.DeleteAny,
                NetptunePermissions.Comments.Create,
            ],
        };

        db.AppUsers.AddRange(owner, assignee, executionUser);
        db.Workspaces.Add(workspace);
        db.ServiceAccounts.Add(serviceAccount);
        db.WorkspaceAppUsers.Add(serviceMembership);
        db.Statuses.AddRange(statuses);
        db.Projects.Add(project);
        db.ProjectTasks.Add(task);

        await db.SaveChangesAsync();

        return new AutomationScenario(workspace, project, task, owner, assignee, executionUser);
    }

    public static async Task<AutomationRule> CreateStatusChangedRule(
        DataContext db,
        AutomationScenario scenario,
        string statusKey,
        AutomationActionType actionType = AutomationActionType.FlagTask)
    {
        var statusId = await GetStatusId(db, scenario, statusKey);

        return await CreateRule(db, scenario, AutomationTriggerType.TaskStatusChanged, new
        {
            statusId,
        }, actionType);
    }

    public static async Task<AutomationRule> CreateTaskChangedRule(
        DataContext db,
        AutomationScenario scenario,
        IReadOnlyCollection<TaskChangeField> fields,
        string? statusKey = null,
        AssigneeChangeMode? assigneeChangeMode = null,
        IReadOnlyCollection<AutomationFieldCondition>? conditions = null,
        AutomationConditionGroup? conditionGroup = null,
        AutomationActionType actionType = AutomationActionType.FlagTask)
    {
        var statusId = statusKey is null ? (int?)null : await GetStatusId(db, scenario, statusKey);

        return await CreateRule(db, scenario, AutomationTriggerType.TaskChanged, new
        {
            fields,
            statusId,
            assigneeChangeMode,
            conditions,
            conditionGroup,
        }, actionType);
    }

    public static async Task<AutomationRule> CreateUnassignedRule(
        DataContext db,
        AutomationScenario scenario,
        int durationDays,
        AutomationActionType actionType = AutomationActionType.FlagTask)
    {
        return await CreateRule(db, scenario, AutomationTriggerType.TaskUnassignedFor, new
        {
            durationDays,
        }, actionType);
    }

    public static async Task<AutomationRule> CreateDueDateRule(
        DataContext db,
        AutomationScenario scenario,
        int durationDays,
        AutomationActionType actionType = AutomationActionType.FlagTask)
    {
        return await CreateRule(db, scenario, AutomationTriggerType.TaskDueDateApproaching, new
        {
            durationDays,
        }, actionType);
    }

    private static async Task<AutomationRule> CreateRule(
        DataContext db,
        AutomationScenario scenario,
        AutomationTriggerType triggerType,
        object triggerConfig,
        AutomationActionType actionType)
    {
        var statusId = actionType == AutomationActionType.UpdateTask
            ? await GetStatusId(db, scenario, "complete")
            : (int?)null;

        var rule = new AutomationRule
        {
            Name = "Automation Rule",
            IsEnabled = true,
            TriggerType = triggerType,
            TriggerConfig = JsonSerializer.SerializeToDocument(triggerConfig, JsonOptions.Default),
            WorkspaceId = scenario.Workspace.Id,
            ExecutionUserId = scenario.ExecutionUser.Id,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
            Actions =
            {
                new AutomationAction
                {
                    Type = actionType,
                    SortOrder = 1,
                    Config = CreateActionConfig(actionType, statusId),
                    OwnerId = scenario.Owner.Id,
                    CreatedByUserId = scenario.Owner.Id,
                },
            },
        };

        db.AutomationRules.Add(rule);
        await db.SaveChangesAsync();

        return rule;
    }

    private static JsonDocument CreateActionConfig(AutomationActionType actionType, int? statusId)
    {
        return actionType switch
        {
            AutomationActionType.NotifyTaskAssignees => JsonSerializer.SerializeToDocument(new
            {
                message = "Automation matched",
            }),
            AutomationActionType.FlagTask => JsonSerializer.SerializeToDocument(new
            {
                flagName = "Needs attention",
                flagDescription = "Flagged by test automation",
            }),
            AutomationActionType.UpdateTask => JsonSerializer.SerializeToDocument(new
            {
                statusId,
                priority = TaskPriority.High,
            }),
            AutomationActionType.AddComment => JsonSerializer.SerializeToDocument(new
            {
                comment = "Added by test automation",
            }),
            AutomationActionType.DeleteTask => JsonSerializer.SerializeToDocument(new { }),
            _ => JsonSerializer.SerializeToDocument(new { }),
        };
    }

    public static Task<int> GetStatusId(DataContext db, AutomationScenario scenario, string key)
    {
        return db.Statuses
            .Where(status =>
                status.WorkspaceId == scenario.Workspace.Id &&
                status.EntityType == EntityType.Task &&
                status.Key == key)
            .Select(status => status.Id)
            .SingleAsync();
    }

    private static AppUser CreateUser(string id, string email)
    {
        return new AppUser
        {
            Id = id,
            Firstname = id,
            Lastname = "User",
            UserName = email,
            NormalizedUserName = email.ToUpperInvariant(),
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            EmailConfirmed = true,
        };
    }
}
