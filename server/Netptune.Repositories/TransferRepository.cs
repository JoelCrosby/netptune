using Dapper;

using Netptune.Core.Repositories.Common;
using Netptune.Repositories.RowMaps;
using Netptune.Repositories.Sql;
using Netptune.Transfer;
using Netptune.Transfer.Repositories;

namespace Netptune.Repositories;

public sealed class TransferRepository(IDbConnectionFactory connectionFactory) : ITransferRepository
{
    public async Task<List<TransferTaskRow>> GetTaskPage(TransferTaskFilter filter, int afterId, int take, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.StartConnection();

        var term = filter.Term?.Trim().ToLowerInvariant() ?? string.Empty;
        var parameters = new
        {
            workspaceId = filter.WorkspaceId,
            includeDeleted = filter.IncludeDeleted,
            afterId,
            take,
            projectKeys = Lowercase(filter.ProjectKeys),
            boardIdentifiers = Lowercase(filter.BoardIdentifiers),
            statusKeys = Lowercase(filter.StatusKeys),
            statusCategories = filter.StatusCategories,
            priorities = filter.Priorities.Select(priority => (int)priority).ToArray(),
            sprintId = filter.SprintId,
            tags = Lowercase(filter.Tags),
            assigneeEmails = Lowercase(filter.AssigneeEmails),
            term,
            termPattern = $"%{term}%",
            createdFrom = filter.CreatedFrom,
            createdTo = filter.CreatedTo,
            updatedSince = filter.UpdatedSince,
        };
        var command = new CommandDefinition(SqlScripts.GetTransferTasks, parameters, cancellationToken: cancellationToken);
        var rows = await connection.QueryAsync<TransferTaskRowMap>(command);

        return rows.Select(row => row.ToRow()).ToList();
    }

    public async Task<int?> ResolveSprintId(int workspaceId, string sprintRef, CancellationToken cancellationToken = default)
    {
        using var connection = connectionFactory.StartConnection();

        const string sql = """
            SELECT s.id, s.name, p.key AS project_key
            FROM sprints s
                     INNER JOIN projects p ON s.project_id = p.id
            WHERE s.workspace_id = @workspaceId
              AND NOT s.is_deleted
            """;
        var command = new CommandDefinition(sql, new { workspaceId }, cancellationToken: cancellationToken);
        var sprints = await connection.QueryAsync<SprintRefRow>(command);

        // A ref is built the same way it is written on export, so the match happens here rather than in
        // SQL. One build per sprint: the predicate used to construct the ref twice for every row it saw.
        foreach (var sprint in sprints)
        {
            var reference = EntityRefBuilder.ForSprint(sprint.Project_Key, sprint.Name);

            if (reference.ToString() == sprintRef || reference.Value == sprintRef)
            {
                return sprint.Id;
            }
        }

        return null;
    }

    private static string[] Lowercase(string[] values)
    {
        return values.Select(value => value.Trim().ToLowerInvariant()).ToArray();
    }

    private sealed record SprintRefRow
    {
        public int Id { get; init; }

        public string Name { get; init; } = null!;

        public string Project_Key { get; init; } = null!;
    }
}
