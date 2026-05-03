namespace Netptune.Core.Requests;

public sealed class TaskFilter
{
    public int? ProjectId { get; init; }

    public int? ExcludeSprintId { get; init; }

    public string? Search { get; init; }

    public int? Take { get; init; }
}
