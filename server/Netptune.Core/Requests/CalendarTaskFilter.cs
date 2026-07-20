namespace Netptune.Core.Requests;

public sealed class CalendarTaskFilter : PageRequest
{
    public required DateOnly Date { get; init; }

    public int? ProjectId { get; init; }

    public int? SprintId { get; init; }
}
