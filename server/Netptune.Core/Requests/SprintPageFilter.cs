namespace Netptune.Core.Requests;

public sealed class SprintPageFilter : PageRequest
{
    public string? Search { get; init; }

    public int? ProjectId { get; init; }
}
