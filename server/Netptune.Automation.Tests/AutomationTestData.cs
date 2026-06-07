using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;
using Netptune.Entities.Contexts;

namespace Netptune.Automation.Tests;

internal static class AutomationTestData
{
    public const string OwnerUserId = "owner-user";
    public const string AssigneeUserId = "assignee-user";

    public static async Task<AutomationScenario> CreateScenario(
        DataContext db,
        ProjectTaskStatus taskStatus = ProjectTaskStatus.New,
        bool assignTask = true,
        DateTime? taskUpdatedAt = null)
    {
        var owner = CreateUser(OwnerUserId, "owner@example.test");
        var assignee = CreateUser(AssigneeUserId, "assignee@example.test");
        var workspace = new Workspace
        {
            Name = "Automation Workspace",
            Slug = "automation-workspace",
            MetaInfo = new WorkspaceMeta(),
            OwnerId = owner.Id,
            CreatedByUserId = owner.Id,
        };

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
            Status = taskStatus,
            ProjectScopeId = 1,
            Project = project,
            Workspace = workspace,
            OwnerId = owner.Id,
            CreatedByUserId = owner.Id,
            UpdatedAt = taskUpdatedAt,
        };

        if (assignTask)
        {
            task.ProjectTaskAppUsers.Add(new ProjectTaskAppUser
            {
                UserId = assignee.Id,
            });
        }

        db.AppUsers.AddRange(owner, assignee);
        db.Workspaces.Add(workspace);
        db.Projects.Add(project);
        db.ProjectTasks.Add(task);

        await db.SaveChangesAsync();

        return new AutomationScenario(workspace, project, task, owner, assignee);
    }

    public static async Task<AutomationRule> CreateStatusChangedRule(
        DataContext db,
        AutomationScenario scenario,
        ProjectTaskStatus status,
        AutomationActionType actionType = AutomationActionType.FlagTask)
    {
        return await CreateRule(db, scenario, AutomationTriggerType.TaskStatusChanged, new
        {
            status = status.ToString(),
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

    private static async Task<AutomationRule> CreateRule(
        DataContext db,
        AutomationScenario scenario,
        AutomationTriggerType triggerType,
        object triggerConfig,
        AutomationActionType actionType)
    {
        var rule = new AutomationRule
        {
            Name = "Automation Rule",
            IsEnabled = true,
            TriggerType = triggerType,
            TriggerConfig = JsonSerializer.SerializeToDocument(triggerConfig),
            WorkspaceId = scenario.Workspace.Id,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
            Actions =
            {
                new AutomationAction
                {
                    Type = actionType,
                    SortOrder = 1,
                    Config = CreateActionConfig(actionType),
                    OwnerId = scenario.Owner.Id,
                    CreatedByUserId = scenario.Owner.Id,
                },
            },
        };

        db.AutomationRules.Add(rule);
        await db.SaveChangesAsync();

        return rule;
    }

    private static JsonDocument CreateActionConfig(AutomationActionType actionType)
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
            _ => JsonSerializer.SerializeToDocument(new { }),
        };
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
