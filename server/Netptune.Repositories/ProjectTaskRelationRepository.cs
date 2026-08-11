using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Enums;
using Netptune.Core.Models.Automations;
using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.ViewModels.Relations;
using Netptune.Entities.Contexts;
using Netptune.Repositories.Common;
using Netptune.Repositories.RowMaps;
using Netptune.Repositories.Sql;

namespace Netptune.Repositories;

public sealed class ProjectTaskRelationRepository : Repository<DataContext, ProjectTaskRelation, int>, IProjectTaskRelationRepository
{
    public ProjectTaskRelationRepository(DataContext context, IDbConnectionFactory connectionFactory)
        : base(context, connectionFactory)
    {
    }

    public async Task<List<TaskRelationViewModel>> GetRelationsForTask(int taskId, int workspaceId, CancellationToken cancellationToken = default)
    {
        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.GetTaskRelations,
            new { TaskId = taskId, WorkspaceId = workspaceId },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<TaskRelationRowMap>(command);

        return rows.Select(row => new TaskRelationViewModel
        {
            Id = row.Relation_Id,
            RelationTypeId = row.Relation_Type_Id,
            RelationTypeName = row.Relation_Type_Name,
            RelationTypeKey = row.Relation_Type_Key,
            RelationTypeColor = row.Relation_Type_Color,
            RelationTypeCategory = row.Relation_Type_Category,

            // The stored edge reads source -> target. Seen from the target, it reads the other way.
            Label = row.Is_Source ? row.Relation_Type_Name : row.Relation_Type_Inverse_Name,
            IsSource = row.Is_Source,

            RelatedTask = new RelatedTaskViewModel
            {
                Id = row.Other_Task_Id,
                SystemId = row.Other_Task_Project_Key is null
                    ? $"{row.Other_Task_Scope_Id}"
                    : $"{row.Other_Task_Project_Key}-{row.Other_Task_Scope_Id}",
                Name = row.Other_Task_Name,
                StatusName = row.Other_Task_Status_Name,
                StatusColor = row.Other_Task_Status_Color,
                StatusCategory = row.Other_Task_Status_Category,
            },
        }).ToList();
    }

    public async Task<PagedResponse<RelationTypeRelationViewModel>> GetRelationsForType(
        int relationTypeId,
        int workspaceId,
        PageRequest pageRequest,
        CancellationToken cancellationToken = default)
    {
        var pagination = pageRequest.GetPagination();

        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.GetRelationsForType,
            new
            {
                RelationTypeId = relationTypeId,
                WorkspaceId = workspaceId,
                Limit = pagination.PageSize,
                Offset = pagination.Skip,
            },
            cancellationToken: cancellationToken);

        var rows = await connection.QueryAsync<RelationTypeRelationRowMap>(command);
        var rowList = rows.ToList();
        var totalCount = rowList.Count == 0 ? 0 : rowList[0].Total_Count;

        var items = rowList.ConvertAll(row => new RelationTypeRelationViewModel
        {
            Id = row.Relation_Id,
            SourceTask = new RelatedTaskViewModel
            {
                Id = row.Source_Task_Id,
                SystemId = BuildSystemId(row.Source_Task_Project_Key, row.Source_Task_Scope_Id),
                Name = row.Source_Task_Name,
                StatusName = row.Source_Task_Status_Name,
                StatusColor = row.Source_Task_Status_Color,
                StatusCategory = row.Source_Task_Status_Category,
                IsArchived = row.Source_Task_Is_Archived,
            },
            TargetTask = new RelatedTaskViewModel
            {
                Id = row.Target_Task_Id,
                SystemId = BuildSystemId(row.Target_Task_Project_Key, row.Target_Task_Scope_Id),
                Name = row.Target_Task_Name,
                StatusName = row.Target_Task_Status_Name,
                StatusColor = row.Target_Task_Status_Color,
                StatusCategory = row.Target_Task_Status_Category,
                IsArchived = row.Target_Task_Is_Archived,
            },
        });

        return new PagedResponse<RelationTypeRelationViewModel>(items, pagination.Page, pagination.PageSize, totalCount);
    }

    private static string BuildSystemId(string? projectKey, int scopeId)
    {
        return projectKey is null ? $"{scopeId}" : $"{projectKey}-{scopeId}";
    }

    public Task<ProjectTaskRelation?> GetInWorkspace(int id, int workspaceId, CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(relation => relation.Id == id && relation.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public Task<bool> Exists(int relationTypeId, int sourceTaskId, int targetTaskId, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(relation =>
            relation.RelationTypeId == relationTypeId &&
            relation.SourceTaskId == sourceTaskId &&
            relation.TargetTaskId == targetTaskId, cancellationToken);
    }

    public Task<bool> HasExistingSource(int relationTypeId, int targetTaskId, CancellationToken cancellationToken = default)
    {
        return Entities.AnyAsync(relation =>
            relation.RelationTypeId == relationTypeId &&
            relation.TargetTaskId == targetTaskId, cancellationToken);
    }

    public Task<List<int>> GetTargetsWithExistingSource(int relationTypeId, IReadOnlyCollection<int> targetTaskIds, CancellationToken cancellationToken = default)
    {
        if (targetTaskIds.Count == 0)
        {
            return Task.FromResult(new List<int>());
        }

        return Entities
            .Where(relation => relation.RelationTypeId == relationTypeId && targetTaskIds.Contains(relation.TargetTaskId))
            .Select(relation => relation.TargetTaskId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> WouldCreateCycle(int relationTypeId, int sourceTaskId, int targetTaskId, CancellationToken cancellationToken = default)
    {
        // The trivial cycle. The database also refuses this via a check constraint, but callers
        // get a clearer message by failing here first.
        if (sourceTaskId == targetTaskId) return true;

        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.CheckTaskRelationCycle,
            new { RelationTypeId = relationTypeId, SourceTaskId = sourceTaskId, TargetTaskId = targetTaskId },
            cancellationToken: cancellationToken);

        return await connection.ExecuteScalarAsync<bool>(command);
    }

    public async Task<List<int>> GetReachableTaskIds(int relationTypeId, IReadOnlyCollection<int> fromTaskIds, CancellationToken cancellationToken = default)
    {
        if (fromTaskIds.Count == 0)
        {
            return [];
        }

        using var connection = ConnectionFactory.StartConnection();

        var command = new CommandDefinition(
            SqlScripts.GetReachableTasks,
            new { RelationTypeId = relationTypeId, FromTaskIds = fromTaskIds.ToArray() },
            cancellationToken: cancellationToken);

        var taskIds = await connection.QueryAsync<int>(command);

        return taskIds.ToList();
    }

    public Task<List<ProjectTaskRelation>> GetForTaskAndType(
        int relationTypeId,
        int taskId,
        int? relatedTaskId,
        CancellationToken cancellationToken = default)
    {
        return Entities
            .Where(relation => relation.RelationTypeId == relationTypeId)
            .Where(relation => relation.SourceTaskId == taskId || relation.TargetTaskId == taskId)
            .Where(relation => relatedTaskId == null
                || relation.SourceTaskId == relatedTaskId
                || relation.TargetTaskId == relatedTaskId)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> DeleteAllByTaskId(IEnumerable<int> taskIds, CancellationToken cancellationToken = default)
    {
        var taskIdList = taskIds.ToList();

        var ids = await Entities
            .Where(relation => taskIdList.Contains(relation.SourceTaskId) || taskIdList.Contains(relation.TargetTaskId))
            .Select(relation => relation.Id)
            .ToListAsync(cancellationToken);

        await DeletePermanent(ids, cancellationToken);

        return ids;
    }

    public Task<List<TaskRelationCounts>> GetBlockerCounts(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
        {
            return Task.FromResult(new List<TaskRelationCounts>());
        }

        return Entities
            .Where(relation =>
                taskIds.Contains(relation.TargetTaskId) &&
                relation.RelationType!.Category == RelationCategory.Dependency &&
                !relation.SourceTask!.IsDeleted)
            .GroupBy(relation => relation.TargetTaskId)
            .Select(group => new TaskRelationCounts(
                group.Key,
                group.Count(),
                group.Count(relation => relation.SourceTask!.Status!.Category != StatusCategory.Done)))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<TaskRelationCounts>> GetChildCounts(
        IReadOnlyCollection<int> taskIds,
        CancellationToken cancellationToken = default)
    {
        if (taskIds.Count == 0)
        {
            return Task.FromResult(new List<TaskRelationCounts>());
        }

        return Entities
            .Where(relation =>
                taskIds.Contains(relation.SourceTaskId) &&
                relation.RelationType!.Category == RelationCategory.Hierarchy &&
                !relation.TargetTask!.IsDeleted)
            .GroupBy(relation => relation.SourceTaskId)
            .Select(group => new TaskRelationCounts(
                group.Key,
                group.Count(),
                group.Count(relation => relation.TargetTask!.Status!.Category != StatusCategory.Done)))
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public Task<List<int>> GetDependentTaskIds(
        IReadOnlyCollection<int> blockingTaskIds,
        CancellationToken cancellationToken = default)
    {
        if (blockingTaskIds.Count == 0)
        {
            return Task.FromResult(new List<int>());
        }

        return Entities
            .Where(relation =>
                blockingTaskIds.Contains(relation.SourceTaskId) &&
                relation.RelationType!.Category == RelationCategory.Dependency &&
                !relation.TargetTask!.IsDeleted)
            .Select(relation => relation.TargetTaskId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<List<int>> GetParentTaskIds(
        IReadOnlyCollection<int> childTaskIds,
        CancellationToken cancellationToken = default)
    {
        if (childTaskIds.Count == 0)
        {
            return Task.FromResult(new List<int>());
        }

        return Entities
            .Where(relation =>
                childTaskIds.Contains(relation.TargetTaskId) &&
                relation.RelationType!.Category == RelationCategory.Hierarchy &&
                !relation.SourceTask!.IsDeleted)
            .Select(relation => relation.SourceTaskId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
