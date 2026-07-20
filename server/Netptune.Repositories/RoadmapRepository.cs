using System.Data;
using System.Text.Json;

using Dapper;

using Netptune.Core.Enums;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Models.Roadmap;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Roadmap;
using Netptune.Core.ViewModels.Users;
using Netptune.Repositories.RowMaps;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class RoadmapRepository(IDbConnectionFactory connectionFactory) : IRoadmapRepository
{
    private const int ScheduledTaskLimit = 2000;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record RoadmapQueryContext
    {
        public required ReportingScope Scope { get; init; }

        public required int[] AllowedProjectIds { get; init; }

        public required int[] ProjectIds { get; init; }

        public required int[] SprintIds { get; init; }
    }

    public async Task<RoadmapViewModel> GetRoadmap(
        ReportingScope scope,
        RoadmapFilter filter,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.StartConnection();

        var context = CreateQueryContext(scope, filter.ProjectIds, filter.SprintIds);

        await ValidateSprintIds(connection, context, cancellationToken);

        var scheduledRows = await GetScheduledTaskRows(
            connection,
            context,
            filter,
            ScheduledTaskLimit + 1,
            cancellationToken);

        var truncated = scheduledRows.Count > ScheduledTaskLimit;
        var scheduledTasks = scheduledRows.Take(ScheduledTaskLimit).Select(ToTaskViewModel).ToList();
        var taskIds = scheduledTasks.Select(task => task.Id).ToArray();

        var relations = await GetRelations(connection, scope.WorkspaceId, taskIds, cancellationToken);
        var sprints = await GetSprints(connection, context, filter, cancellationToken);

        return new RoadmapViewModel
        {
            From = filter.From,
            To = filter.To,
            Tasks = scheduledTasks,
            Relations = relations,
            Sprints = sprints,
            Truncated = truncated,
        };
    }

    public async Task<PagedResponse<RoadmapTaskViewModel>> GetUnscheduledTasks(
        ReportingScope scope,
        RoadmapUnscheduledTaskFilter filter,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.StartConnection();

        var context = CreateQueryContext(scope, filter.ProjectIds, filter.SprintIds);

        await ValidateSprintIds(connection, context, cancellationToken);

        var page = filter.GetPage();
        var pageSize = filter.GetPageSize();
        var skip = (page - 1) * pageSize;
        var taskOrder = GetUnscheduledTaskOrder(filter);
        var sql = SqlScripts.GetUnscheduledRoadmapTasks.Replace("{taskOrder}", taskOrder);
        var command = new CommandDefinition(
            sql,
            new
            {
                workspaceId = context.Scope.WorkspaceId,
                allowedProjectIds = context.AllowedProjectIds,
                projectIds = context.ProjectIds,
                sprintIds = context.SprintIds,
                pageSize,
                skip,
            },
            cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<RoadmapTaskRowMap>(command)).ToList();
        var totalCount = rows.FirstOrDefault()?.Total_Count ?? 0;
        var tasks = rows.Select(ToTaskViewModel).ToList();

        return new PagedResponse<RoadmapTaskViewModel>(tasks, page, pageSize, totalCount);
    }

    public async Task<PagedResponse<RoadmapTaskViewModel>> GetCalendarTasks(
        ReportingScope scope,
        CalendarTaskFilter filter,
        CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.StartConnection();

        var projectIds = filter.ProjectId.HasValue ? new[] { filter.ProjectId.Value } : [];
        var sprintIds = filter.SprintId.HasValue ? new[] { filter.SprintId.Value } : [];
        var context = CreateQueryContext(scope, projectIds, sprintIds);

        await ValidateSprintIds(connection, context, cancellationToken);

        var page = filter.GetPage();
        var pageSize = filter.GetPageSize();
        var skip = (page - 1) * pageSize;
        var taskOrder = GetCalendarTaskOrder(filter);
        var sql = SqlScripts.GetCalendarTasks.Replace("{taskOrder}", taskOrder);
        var command = new CommandDefinition(
            sql,
            new
            {
                workspaceId = context.Scope.WorkspaceId,
                allowedProjectIds = context.AllowedProjectIds,
                projectId = filter.ProjectId,
                sprintId = filter.SprintId,
                date = filter.Date.ToDateTime(TimeOnly.MinValue),
                pageSize,
                skip,
            },
            cancellationToken: cancellationToken);
        var rows = (await connection.QueryAsync<RoadmapTaskRowMap>(command)).ToList();
        var totalCount = rows.FirstOrDefault()?.Total_Count ?? 0;
        var tasks = rows.Select(ToTaskViewModel).ToList();

        return new PagedResponse<RoadmapTaskViewModel>(tasks, page, pageSize, totalCount);
    }

    private static RoadmapQueryContext CreateQueryContext(
        ReportingScope scope,
        IReadOnlyCollection<int> projectIds,
        IReadOnlyCollection<int> sprintIds)
    {
        return new RoadmapQueryContext
        {
            Scope = scope,
            AllowedProjectIds = scope.ProjectIds.OrderBy(id => id).ToArray(),
            ProjectIds = projectIds.Distinct().OrderBy(id => id).ToArray(),
            SprintIds = sprintIds.Distinct().OrderBy(id => id).ToArray(),
        };
    }

    private static async Task ValidateSprintIds(
        IDbConnection connection,
        RoadmapQueryContext context,
        CancellationToken cancellationToken)
    {
        if (context.SprintIds.Length == 0)
        {
            return;
        }

        var command = new CommandDefinition(
            SqlScripts.GetRoadmapSprintIds,
            new
            {
                workspaceId = context.Scope.WorkspaceId,
                allowedProjectIds = context.AllowedProjectIds,
                sprintIds = context.SprintIds,
            },
            cancellationToken: cancellationToken);

        var validSprintIds = (await connection.QueryAsync<int>(command)).ToHashSet();
        var containsUnknownSprint = context.SprintIds.Any(id => !validSprintIds.Contains(id));

        if (containsUnknownSprint)
        {
            throw new InvalidRoadmapFilterException("One or more sprints are outside the current workspace scope");
        }
    }

    private static async Task<List<RoadmapTaskRowMap>> GetScheduledTaskRows(
        IDbConnection connection,
        RoadmapQueryContext context,
        RoadmapFilter filter,
        int take,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            SqlScripts.GetRoadmapTasks,
            new
            {
                workspaceId = context.Scope.WorkspaceId,
                allowedProjectIds = context.AllowedProjectIds,
                projectIds = context.ProjectIds,
                sprintIds = context.SprintIds,
                from = filter.From.ToDateTime(TimeOnly.MinValue),
                to = filter.To.ToDateTime(TimeOnly.MinValue),
                take,
            },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<RoadmapTaskRowMap>(command);

        return rows.ToList();
    }

    private static async Task<IReadOnlyList<RoadmapRelationViewModel>> GetRelations(
        IDbConnection connection,
        int workspaceId,
        int[] taskIds,
        CancellationToken cancellationToken)
    {
        if (taskIds.Length == 0)
        {
            return [];
        }

        var hierarchyCategory = (int) RelationCategory.Hierarchy;
        var dependencyCategory = (int) RelationCategory.Dependency;
        var categories = new[] { hierarchyCategory, dependencyCategory };

        var command = new CommandDefinition(
            SqlScripts.GetRoadmapRelations,
            new { workspaceId, taskIds, categories, hierarchyCategory, dependencyCategory },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<RoadmapRelationRowMap>(command);

        return rows.Select(row => new RoadmapRelationViewModel
        {
            Id = row.Id,
            SourceTaskId = row.Source_Task_Id,
            TargetTaskId = row.Target_Task_Id,
            RelationTypeId = row.Relation_Type_Id,
            RelationTypeKey = row.Relation_Type_Key,
            Category = row.Category,
        }).ToList();
    }

    private static async Task<IReadOnlyList<RoadmapSprintViewModel>> GetSprints(
        IDbConnection connection,
        RoadmapQueryContext context,
        RoadmapFilter filter,
        CancellationToken cancellationToken)
    {
        var command = new CommandDefinition(
            SqlScripts.GetRoadmapSprints,
            new
            {
                workspaceId = context.Scope.WorkspaceId,
                allowedProjectIds = context.AllowedProjectIds,
                projectIds = context.ProjectIds,
                sprintIds = context.SprintIds,
                from = filter.From.ToDateTime(TimeOnly.MinValue),
                to = filter.To.ToDateTime(TimeOnly.MinValue),
            },
            cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<RoadmapSprintRowMap>(command);

        return rows.Select(row => new RoadmapSprintViewModel
        {
            Id = row.Id,
            Name = row.Name,
            StartDate = row.Start_Date,
            EndDate = row.End_Date,
            Status = row.Status,
            ProjectId = row.Project_Id,
        }).ToList();
    }

    private static string GetUnscheduledTaskOrder(RoadmapUnscheduledTaskFilter filter)
    {
        var direction = string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";
        var expression = filter.SortBy?.Trim() switch
        {
            "systemId" => "CONCAT_WS('-', p.key, pt.project_scope_id::text)",
            "name" => "pt.name",
            "projectName" => "p.name",
            "statusName" => "st.name",
            "priority" => "pt.priority",
            "assignees" => "(SELECT COUNT(*) FROM project_task_app_users sort_ptau " +
                "WHERE sort_ptau.project_task_id = pt.id)",
            _ => null,
        };

        return expression is null
            ? "p.name, pt.project_scope_id, pt.id"
            : $"{expression} {direction} NULLS LAST, pt.id {direction}";
    }

    private static string GetCalendarTaskOrder(CalendarTaskFilter filter)
    {
        var direction = string.Equals(filter.SortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            ? "ASC"
            : "DESC";
        var expression = filter.SortBy?.Trim() switch
        {
            "systemId" => "CONCAT_WS('-', p.key, pt.project_scope_id::text)",
            "name" => "pt.name",
            "projectName" => "p.name",
            "statusName" => "st.name",
            "schedule" => "COALESCE(pt.start_date, pt.due_date)",
            "assignees" => "(SELECT COUNT(*) FROM project_task_app_users sort_ptau " +
                "WHERE sort_ptau.project_task_id = pt.id)",
            _ => null,
        };

        return expression is null
            ? "p.name, pt.project_scope_id, pt.id"
            : $"{expression} {direction} NULLS LAST, pt.id {direction}";
    }

    private static RoadmapTaskViewModel ToTaskViewModel(RoadmapTaskRowMap row)
    {
        var assigneeRows = JsonSerializer
            .Deserialize<List<RoadmapAssigneeRowMap>>(row.Assignees, JsonOptions) ?? [];

        return new RoadmapTaskViewModel
        {
            Id = row.Task_Id,
            ProjectScopeId = row.Project_Scope_Id,
            SystemId = $"{row.Project_Key}-{row.Project_Scope_Id}",
            Name = row.Task_Name,
            ProjectId = row.Project_Id,
            ProjectName = row.Project_Name,
            ProjectKey = row.Project_Key,
            StatusId = row.Status_Id,
            StatusName = row.Status_Name,
            StatusKey = row.Status_Key,
            StatusColor = row.Status_Color,
            StatusCategory = row.Status_Category,
            Priority = row.Priority,
            StartDate = row.Start_Date,
            DueDate = row.Due_Date,
            SprintId = row.Sprint_Id,
            Assignees = assigneeRows.Select(assignee => new AssigneeViewModel
            {
                Id = assignee.Id,
                DisplayName = assignee.DisplayName,
                PictureUrl = assignee.PictureUrl,
                IsServiceAccount = assignee.IsServiceAccount,
            }).ToList(),
        };
    }
}
