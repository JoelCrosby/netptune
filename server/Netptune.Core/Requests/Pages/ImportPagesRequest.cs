namespace Netptune.Core.Requests.Pages;

public sealed record ImportPagesRequest
{
    public int? ParentId { get; init; }

    public int? ProjectId { get; init; }
}
