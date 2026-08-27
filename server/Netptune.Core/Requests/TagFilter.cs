namespace Netptune.Core.Requests;

public sealed class TagFilter : PageRequest
{
    public string? Search { get; init; }
}
