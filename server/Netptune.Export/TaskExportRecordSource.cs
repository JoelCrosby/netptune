using Netptune.Transfer.Repositories;
using System.Runtime.CompilerServices;

using Netptune.Transfer.Services;
using Netptune.Transfer;
using Netptune.Transfer.Export;

namespace Netptune.Export;

public sealed class TaskExportRecordSource : IExportRecordSource
{
    public const int PageSize = 5000;

    private readonly ITransferRepository Transfers;

    public TaskExportRecordSource(ITransferRepository transfers)
    {
        Transfers = transfers;
    }

    public bool CanRead(string recordType)
    {
        return string.Equals(recordType, EntityRefTypes.Task, StringComparison.OrdinalIgnoreCase);
    }

    public IReadOnlyList<TransferField> ResolveFields(ExportDefinitionModel definition)
    {
        return ExportDefinitionValidator.ResolveFields(definition);
    }

    public async IAsyncEnumerable<ExportRecord> Read(
        ExportRecordQuery query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var fields = ResolveFields(query.Definition);
        var filter = await BuildFilter(query, cancellationToken);
        var afterId = 0;
        var emitted = 0L;
        var hasMore = true;

        while (hasMore)
        {
            var take = ResolvePageSize(query.MaxRecords, emitted);

            if (take == 0)
            {
                yield break;
            }

            var rows = await Transfers.GetTaskPage(filter, afterId, take, cancellationToken);

            foreach (var row in rows)
            {
                yield return ToRecord(row, fields);
            }

            emitted += rows.Count;
            hasMore = rows.Count == take;

            if (hasMore)
            {
                afterId = rows[^1].Id;
            }
        }
    }

    public async Task<long> EstimateCount(ExportRecordQuery query, CancellationToken cancellationToken = default)
    {
        var filter = await BuildFilter(query, cancellationToken);
        var rows = await Transfers.GetTaskPage(filter, 0, 1, cancellationToken);
        var total = rows.Count == 0 ? 0 : rows[0].TotalCount;

        if (query.MaxRecords is null)
        {
            return total;
        }

        return Math.Min(total, query.MaxRecords.Value);
    }

    private async Task<TransferTaskFilter> BuildFilter(ExportRecordQuery query, CancellationToken cancellationToken)
    {
        var filter = query.Definition.Filter;

        if (filter is null)
        {
            return new TransferTaskFilter { WorkspaceId = query.WorkspaceId };
        }

        var sprintId = await ResolveSprintId(query.WorkspaceId, filter.SprintRef, cancellationToken);

        return new TransferTaskFilter
        {
            WorkspaceId = query.WorkspaceId,
            IncludeDeleted = filter.IncludeDeleted,
            ProjectKeys = [.. filter.ProjectKeys],
            BoardIdentifiers = [.. filter.BoardIdentifiers],
            StatusKeys = [.. filter.StatusKeys],
            StatusCategories = [.. filter.StatusCategories],
            Tags = [.. filter.Tags],
            AssigneeEmails = [.. filter.AssigneeEmails],
            Priorities = [.. filter.Priorities],
            SprintId = sprintId,
            Term = filter.Term,
            CreatedFrom = filter.CreatedFrom,
            CreatedTo = filter.CreatedTo,
            UpdatedSince = filter.UpdatedSince,
        };
    }

    private async Task<int?> ResolveSprintId(int workspaceId, string? sprintRef, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sprintRef))
        {
            return null;
        }

        return await Transfers.ResolveSprintId(workspaceId, sprintRef, cancellationToken);
    }

    private static int ResolvePageSize(int? maxRecords, long emitted)
    {
        if (maxRecords is null)
        {
            return PageSize;
        }

        var remaining = maxRecords.Value - emitted;

        if (remaining <= 0)
        {
            return 0;
        }

        return (int)Math.Min(PageSize, remaining);
    }

    private static ExportRecord ToRecord(TransferTaskRow row, IReadOnlyList<TransferField> fields)
    {
        var projectKey = row.ProjectKey ?? string.Empty;
        var taskRef = EntityRefBuilder.ForTask(projectKey, row.ProjectScopeId);
        var values = new Dictionary<string, object?>(fields.Count, StringComparer.OrdinalIgnoreCase);

        foreach (var field in fields)
        {
            values[field.Key] = ResolveValue(field.Key, row, taskRef);
        }

        return new ExportRecord
        {
            Ref = taskRef,
            Values = values,
        };
    }

    private static object? ResolveValue(string fieldKey, TransferTaskRow row, EntityRef taskRef)
    {
        return fieldKey switch
        {
            TaskFieldKeys.SystemId => taskRef.Value,
            TaskFieldKeys.Name => row.Name,
            TaskFieldKeys.Description => row.Description,
            TaskFieldKeys.Status => ResolveRef(row.StatusKey, EntityRefBuilder.ForStatus),
            TaskFieldKeys.Priority => row.Priority,
            TaskFieldKeys.EstimateType => row.EstimateType,
            TaskFieldKeys.EstimateValue => row.EstimateValue,
            TaskFieldKeys.StartDate => row.StartDate,
            TaskFieldKeys.DueDate => row.DueDate,
            TaskFieldKeys.Project => ResolveRef(row.ProjectKey, EntityRefBuilder.ForProject),
            TaskFieldKeys.Sprint => ResolveSprintRef(row),
            TaskFieldKeys.BoardGroup => ResolveBoardGroupRef(row),
            TaskFieldKeys.Assignees => row.AssigneeEmails.Select(EntityRefBuilder.ForUser).ToList(),
            TaskFieldKeys.Tags => row.TagNames.Select(EntityRefBuilder.ForTag).ToList(),
            TaskFieldKeys.CreatedBy => ResolveRef(row.CreatedByEmail, EntityRefBuilder.ForUser),
            TaskFieldKeys.CreatedAt => row.CreatedAt,
            TaskFieldKeys.UpdatedAt => row.UpdatedAt,
            _ => null,
        };
    }

    private static EntityRef? ResolveRef(string? value, Func<string, EntityRef> build)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return build(value);
    }

    private static EntityRef? ResolveSprintRef(TransferTaskRow row)
    {
        var hasSprint = !string.IsNullOrWhiteSpace(row.SprintName) && !string.IsNullOrWhiteSpace(row.SprintProjectKey);

        if (!hasSprint)
        {
            return null;
        }

        return EntityRefBuilder.ForSprint(row.SprintProjectKey!, row.SprintName!);
    }

    private static EntityRef? ResolveBoardGroupRef(TransferTaskRow row)
    {
        var hasPlacement = !string.IsNullOrWhiteSpace(row.BoardIdentifier) && !string.IsNullOrWhiteSpace(row.BoardGroupName);

        if (!hasPlacement)
        {
            return null;
        }

        return EntityRefBuilder.ForBoardGroup(row.BoardIdentifier!, row.BoardGroupName!);
    }
}
