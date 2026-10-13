namespace Netptune.Core.Requests;

public sealed class ProjectFilter : PageRequest
{
    public string? Search { get; init; }
}
