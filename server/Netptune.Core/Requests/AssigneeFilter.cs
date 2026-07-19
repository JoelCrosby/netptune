namespace Netptune.Core.Requests;

public sealed class AssigneeFilter : PageRequest
{
    public string? Search { get; init; }
}
