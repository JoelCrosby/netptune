namespace Netptune.Core.Requests.Pages;

public sealed record SavePageRequest
{
    public string? Title { get; init; }

    public string? Body { get; init; }

    public int Revision { get; init; }
}
