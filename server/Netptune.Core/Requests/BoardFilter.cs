namespace Netptune.Core.Requests;

public sealed class BoardFilter : PageRequest
{
    public string? Search { get; init; }
}
