using System.Data;
using System.Text.Json;

using Dapper;

using Netptune.Core.Enums;
using Netptune.Core.Models.Reporting;
using Netptune.Core.Models.Roadmap;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Roadmap;
using Netptune.Core.ViewModels.Users;
using Netptune.Repositories.RowMaps;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class RoadmapRepository(IDbConnectionFactory connectionFactory) : IRoadmapRepository
{
    private const int ScheduledTaskLimit = 2000;
    private const int UnscheduledTaskLimit = 200;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private sealed record RoadmapQueryContext
    {
        public required ReportingScope Scope { get; init; }

        public required RoadmapFilter Filter { get; init; }

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

        var context = new RoadmapQueryContext
        {
            Scope = scope,
            Filter = filter,
            AllowedProjectIds = scope.ProjectIds.OrderBy(id => id).ToArray(),
            ProjectIds = filter.ProjectIds.Distinct().OrderBy(id => id).ToArray(),
            SprintIds = filter.SprintIds.Distinct().OrderBy(id => id).ToArray(),
        };

        await ValidateSprintIds(connection, context, cancellationToken);

        var scheduledRows = await GetTaskRows(
            connection,
            context,
            false,
            ScheduledTaskLimit + 1,
            cancellationToken);

        var truncated = scheduledRows.Count > ScheduledTaskLimit;
        var scheduledTasks = scheduledRows.Take(ScheduledTaskLimit).Select(ToTaskViewModel).ToList();
        var unscheduledRows = filter.IncludeUnscheduled
            ? await GetTaskRows(connection, context, true, UnscheduledTaskLimit, cancellationToken)
            : [];

        var unscheduledTasks = unscheduledRows.Select(ToTaskViewModel).ToList();
        var taskIds = scheduledTasks.Select(task => task.Id)
            .Concat(unscheduledTasks.Select(task => task.Id))
            .Distinct()
            .ToArray();

        var relations = await GetRelations(connection, scope.WorkspaceId, taskIds, cancellationToken);
        var sprints = await GetSprints(connection, context, cancellationToken);
        var unscheduledCount = unscheduledRows.FirstOrDefault()?.Total_Count ?? 0;

        return new RoadmapViewModel
        {
            From = filter.From,
            To = filter.To,
            Tasks = scheduledTasks,
            Relations = relations,
            Sprints = sprints,
            UnscheduledTasks = unscheduledTasks,
            UnscheduledCount = unscheduledCount,
            Truncated = truncated,
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

    private static async Task<List<RoadmapTaskRowMap>> GetTaskRows(
        IDbConnection connection,
        RoadmapQueryContext context,
        bool unscheduled,
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
                from = context.Filter.From.ToDateTime(TimeOnly.MinValue),
                to = context.Filter.To.ToDateTime(TimeOnly.MinValue),
                unscheduled,
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

        var categories = new[] { (int)RelationCategory.Hierarchy, (int)RelationCategory.Dependency };
        var command = new CommandDefinition(
            SqlScripts.GetRoadmapRelations,
            new { workspaceId, taskIds, categories },
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
                from = context.Filter.From.ToDateTime(TimeOnly.MinValue),
                to = context.Filter.To.ToDateTime(TimeOnly.MinValue),
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
            }).ToList(),
        };
    }
}
