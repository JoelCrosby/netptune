namespace Netptune.Core.Requests.Ai;

public sealed record InlineWriteRequest
{
    public string? Prompt { get; init; }

    public string? Before { get; init; }

    public string? After { get; init; }
}
