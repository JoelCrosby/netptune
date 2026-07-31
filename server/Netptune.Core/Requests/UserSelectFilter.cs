namespace Netptune.Core.Requests;

public sealed class UserSelectFilter : PageRequest
{
    public string? Search { get; init; }
}
