using Dapper;

using Microsoft.EntityFrameworkCore;

using Netptune.Core.Relationships;
using Netptune.Core.Repositories;
using Netptune.Core.Repositories.Common;
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
}
