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
            NextTaskScopeId = 2,
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
                NetptunePermissions.Tasks.Create,
                NetptunePermissions.Tasks.Update,
                NetptunePermissions.Tasks.Move,
                NetptunePermissions.Tasks.Reassign,
                NetptunePermissions.Tasks.DeleteAny,
                NetptunePermissions.Sprints.ManageTasks,
                NetptunePermissions.Comments.Create,
                NetptunePermissions.Tags.Assign,
            ],
        };

        var ownerMembership = new WorkspaceAppUser
        {
            UserId = owner.Id,
            Workspace = workspace,
            Role = WorkspaceRole.Admin,
            Permissions = [],
        };
        var assigneeMembership = new WorkspaceAppUser
        {
            UserId = assignee.Id,
            Workspace = workspace,
            Role = WorkspaceRole.Member,
            Permissions = [],
        };

        var board = new Board
        {
            Name = "Automation Board",
            Identifier = "automation-default-board",
            Project = project,
            BoardType = BoardType.Default,
            MetaInfo = new BoardMeta(),
            Workspace = workspace,
            OwnerId = owner.Id,
            CreatedByUserId = owner.Id,
        };
        var boardGroup = new BoardGroup
        {
            Name = "Backlog",
            Board = board,
            SortOrder = 1,
            Workspace = workspace,
            OwnerId = owner.Id,
            CreatedByUserId = owner.Id,
        };
        db.AppUsers.AddRange(owner, assignee, executionUser);
        db.Workspaces.Add(workspace);
        db.ServiceAccounts.Add(serviceAccount);
        db.WorkspaceAppUsers.AddRange(serviceMembership, ownerMembership, assigneeMembership);
        db.Statuses.AddRange(statuses);

        db.Projects.Add(project);
        db.Boards.Add(board);
        db.BoardGroups.Add(boardGroup);
        db.ProjectTasks.Add(task);

        await db.SaveChangesAsync();

        return new AutomationScenario(workspace, project, task, owner, assignee, executionUser);
    }

    public static async Task<AutomationRule> CreateTaskChangedRule(
        DataContext db,
        AutomationScenario scenario,
        IReadOnlyCollection<TaskChangeField> fields,
        AutomationConditionGroup? conditionGroup = null,
        AutomationActionType actionType = AutomationActionType.FlagTask)
    {
        var rule = await CreateRule(db, scenario, AutomationTriggerType.TaskChanged, new
        {
            fields,
            conditionGroup,
        }, actionType);

        return rule;
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

    public static async Task<AutomationRule> CreateTaskStateRule(
        DataContext db,
        AutomationScenario scenario,
        AutomationTriggerType triggerType,
        int? durationDays = null,
        AutomationConditionGroup? conditionGroup = null,
        AutomationActionType actionType = AutomationActionType.FlagTask)
    {
        return await CreateRule(db, scenario, triggerType, new
        {
            durationDays,
            conditionGroup,
        }, actionType);
    }

    public static async Task AddProjectMember(DataContext db, AutomationScenario scenario, string userId)
    {
        db.ProjectUsers.Add(new ProjectUser
        {
            ProjectId = scenario.Project.Id,
            UserId = userId,
        });

        await db.SaveChangesAsync();
    }

    public static async Task ScopeRule(
        DataContext db,
        AutomationRule rule,
        int? projectId = null,
        int? boardId = null,
        int? sprintId = null)
    {
        var tracked = await db.AutomationRules.SingleAsync(candidate => candidate.Id == rule.Id);

        tracked.ProjectId = projectId;
        tracked.BoardId = boardId;
        tracked.SprintId = sprintId;

        await db.SaveChangesAsync();
    }

    public static async Task<Project> CreateProject(DataContext db, AutomationScenario scenario, string key)
    {
        var project = new Project
        {
            Name = $"Project {key}",
            Key = key,
            WorkspaceId = scenario.Workspace.Id,
            MetaInfo = new ProjectMeta(),
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        db.Projects.Add(project);
        await db.SaveChangesAsync();

        return project;
    }

    public static async Task<Sprint> CreateSprint(
        DataContext db,
        AutomationScenario scenario,
        string name,
        int? projectId = null,
        SprintStatus status = SprintStatus.Active)
    {
        var sprint = new Sprint
        {
            Name = name,
            Status = status,
            StartDate = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 7, 14, 0, 0, 0, DateTimeKind.Utc),
            ProjectId = projectId ?? scenario.Project.Id,
            WorkspaceId = scenario.Workspace.Id,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        db.Sprints.Add(sprint);
        await db.SaveChangesAsync();

        return sprint;
    }

    public static async Task<Board> CreateBoard(DataContext db, AutomationScenario scenario, string identifier)
    {
        var board = new Board
        {
            Name = $"Board {identifier}",
            Identifier = identifier,
            ProjectId = scenario.Project.Id,
            BoardType = BoardType.UserDefined,
            MetaInfo = new BoardMeta(),
            WorkspaceId = scenario.Workspace.Id,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        db.Boards.Add(board);
        await db.SaveChangesAsync();

        return board;
    }

    public static async Task<RelationType> CreateRelationType(
        DataContext db,
        AutomationScenario scenario,
        RelationCategory category = RelationCategory.Hierarchy)
    {
        var isHierarchy = category == RelationCategory.Hierarchy;
        var relationType = new RelationType
        {
            Name = isHierarchy ? "Parent of" : "Blocks",
            InverseName = isHierarchy ? "Child of" : "Is blocked by",
            Key = isHierarchy ? "parent-of" : "blocks",
            Category = category,
            IsSystem = true,
            SortOrder = 1,
            WorkspaceId = scenario.Workspace.Id,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        db.RelationTypes.Add(relationType);
        await db.SaveChangesAsync();

        return relationType;
    }

    public static async Task AssignTaskToSprint(DataContext db, int taskId, int sprintId)
    {
        await db.ProjectTasks
            .Where(candidate => candidate.Id == taskId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.SprintId, sprintId));

        db.ChangeTracker.Clear();
    }

    public static async Task SetTaskStatus(DataContext db, int taskId, int statusId)
    {
        await db.ProjectTasks
            .Where(candidate => candidate.Id == taskId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.StatusId, statusId));

        db.ChangeTracker.Clear();
    }

    public static async Task SetSprintEndDate(DataContext db, int sprintId, DateTime endDate)
    {
        await db.Sprints
            .Where(candidate => candidate.Id == sprintId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(sprint => sprint.EndDate, endDate));

        db.ChangeTracker.Clear();
    }

    public static async Task CreateRuns(
        DataContext db,
        AutomationRule rule,
        int count,
        AutomationRunStatus status)
    {
        var runs = Enumerable.Range(0, count).Select(index => new AutomationRun
        {
            AutomationRuleId = rule.Id,
            TriggerType = rule.TriggerType,
            Status = status,
            IdempotencyKey = $"seed:{rule.Id}:{status}:{index}:{Guid.NewGuid():N}",
            EntityType = EntityType.Task,
            OwnerId = rule.OwnerId,
            CreatedByUserId = rule.CreatedByUserId,
        });

        db.AutomationRuns.AddRange(runs);

        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }

    public static async Task SoftDeleteTask(DataContext db, int taskId)
    {
        await db.ProjectTasks
            .Where(candidate => candidate.Id == taskId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(task => task.IsDeleted, true));

        db.ChangeTracker.Clear();
    }

    public static async Task SetSprintStatus(DataContext db, int sprintId, SprintStatus status)
    {
        await db.Sprints
            .Where(candidate => candidate.Id == sprintId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(sprint => sprint.Status, status));

        db.ChangeTracker.Clear();
    }

    public static async Task<ProjectTask> CreateTask(
        DataContext db,
        AutomationScenario scenario,
        string name,
        int? projectId = null)
    {
        var targetProjectId = projectId ?? scenario.Project.Id;
        var project = await db.Projects.SingleAsync(candidate => candidate.Id == targetProjectId);
        var task = new ProjectTask
        {
            Name = name,
            StatusId = scenario.Task.StatusId,
            ProjectId = project.Id,
            ProjectScopeId = project.NextTaskScopeId,
            WorkspaceId = scenario.Workspace.Id,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
        };

        project.NextTaskScopeId++;
        db.ProjectTasks.Add(task);
        await db.SaveChangesAsync();

        return task;
    }

    public static async Task<ProjectTaskRelation> CreateRelation(
        DataContext db,
        AutomationScenario scenario,
        RelationType relationType,
        int sourceTaskId,
        int targetTaskId)
    {
        var relation = new ProjectTaskRelation
        {
            WorkspaceId = scenario.Workspace.Id,
            RelationTypeId = relationType.Id,
            SourceTaskId = sourceTaskId,
            TargetTaskId = targetTaskId,
        };

        db.ProjectTaskRelations.Add(relation);
        await db.SaveChangesAsync();

        return relation;
    }

    public static async Task<AutomationRule> CreateRelationRule(
        DataContext db,
        AutomationScenario scenario,
        object actionConfig)
    {
        return await CreateActionRule(db, scenario, AutomationActionType.ManageTaskRelation, actionConfig);
    }

    public static async Task<AutomationRule> CreateCreateTaskRule(        DataContext db,
        AutomationScenario scenario,
        object actionConfig)
    {
        return await CreateActionRule(db, scenario, AutomationActionType.CreateTask, actionConfig);
    }

    private static async Task<AutomationRule> CreateActionRule(
        DataContext db,
        AutomationScenario scenario,
        AutomationActionType actionType,
        object actionConfig)
    {
        var rule = new AutomationRule
        {
            Name = "Automation Rule",
            IsEnabled = true,
            TriggerType = AutomationTriggerType.TaskChanged,
            TriggerConfig = JsonSerializer.SerializeToDocument(new
            {
                fields = new[] { TaskChangeField.Status },
            }, JsonOptions.Default),
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
                    Config = JsonSerializer.SerializeToDocument(actionConfig, JsonOptions.Default),
                    OwnerId = scenario.Owner.Id,
                    CreatedByUserId = scenario.Owner.Id,
                },
            },
        };

        db.AutomationRules.Add(rule);
        await db.SaveChangesAsync();

        return rule;
    }

    public static async Task<AutomationRule> CreateNotifyRule(
        DataContext db,
        AutomationScenario scenario,
        IReadOnlyCollection<AutomationNotificationRecipient> recipients,
        string? message = null,
        IReadOnlyCollection<string>? recipientUserIds = null,
        IReadOnlyCollection<WorkspaceRole>? recipientRoles = null)
    {
        var triggerConfig = new
        {
            fields = new[] { TaskChangeField.Status },
        };
        var actionConfig = JsonSerializer.SerializeToDocument(new
        {
            message,
            recipients,
            recipientUserIds = recipientUserIds ?? [],
            recipientRoles = recipientRoles ?? [],
        }, JsonOptions.Default);

        var rule = new AutomationRule
        {
            Name = "Automation Rule",
            IsEnabled = true,
            TriggerType = AutomationTriggerType.TaskChanged,
            TriggerConfig = JsonSerializer.SerializeToDocument(triggerConfig, JsonOptions.Default),
            WorkspaceId = scenario.Workspace.Id,
            ExecutionUserId = scenario.ExecutionUser.Id,
            OwnerId = scenario.Owner.Id,
            CreatedByUserId = scenario.Owner.Id,
            Actions =
            {
                new AutomationAction
                {
                    Type = AutomationActionType.NotifyTaskAssignees,
                    SortOrder = 1,
                    Config = actionConfig,
                    OwnerId = scenario.Owner.Id,
                    CreatedByUserId = scenario.Owner.Id,
                },
            },
        };

        db.AutomationRules.Add(rule);
        await db.SaveChangesAsync();

        return rule;
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
