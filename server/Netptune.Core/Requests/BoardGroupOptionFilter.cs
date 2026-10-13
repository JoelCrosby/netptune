namespace Netptune.Core.Requests;

public sealed class BoardGroupOptionFilter : PageRequest
{
    public string? Search { get; init; }

    public int? BoardId { get; init; }
}
