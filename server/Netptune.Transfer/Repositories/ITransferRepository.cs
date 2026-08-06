using Netptune.Core.Enums;

namespace Netptune.Transfer.Repositories;

public sealed record TransferTaskFilter
{
    public required int WorkspaceId { get; init; }

    public bool IncludeDeleted { get; init; }

    public string[] ProjectKeys { get; init; } = [];

    public string[] BoardIdentifiers { get; init; } = [];

    public string[] StatusKeys { get; init; } = [];

    public int[] StatusCategories { get; init; } = [];

    public string[] Tags { get; init; } = [];

    public string[] AssigneeEmails { get; init; } = [];

    public TaskPriority[] Priorities { get; init; } = [];

    public int? SprintId { get; init; }

    public string? Term { get; init; }

    public DateTime? CreatedFrom { get; init; }

    public DateTime? CreatedTo { get; init; }

    public DateTime? UpdatedSince { get; init; }
}

public sealed record TransferTaskRow
{
    public int Id { get; init; }

    public long TotalCount { get; init; }

    public string? ProjectKey { get; init; }

    public int ProjectScopeId { get; init; }

    public string Name { get; init; } = null!;

    public string? Description { get; init; }

    public string? StatusKey { get; init; }

    public TaskPriority? Priority { get; init; }

    public EstimateType? EstimateType { get; init; }

    public decimal? EstimateValue { get; init; }

    public DateOnly? StartDate { get; init; }

    public DateOnly? DueDate { get; init; }

    public string? SprintName { get; init; }

    public string? SprintProjectKey { get; init; }

    public string? BoardIdentifier { get; init; }

    public string? BoardGroupName { get; init; }

    public string? CreatedByEmail { get; init; }

    public DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public string[] AssigneeEmails { get; init; } = [];

    public string[] TagNames { get; init; } = [];
}

public interface ITransferRepository
{
    Task<List<TransferTaskRow>> GetTaskPage(TransferTaskFilter filter, int afterId, int take, CancellationToken cancellationToken = default);

    Task<int?> ResolveSprintId(int workspaceId, string sprintRef, CancellationToken cancellationToken = default);
}
