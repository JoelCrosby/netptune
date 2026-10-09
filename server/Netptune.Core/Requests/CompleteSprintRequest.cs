namespace Netptune.Core.Requests;

public sealed record CompleteSprintRequest
{
    public int? CarryOverSprintId { get; init; }
}
