namespace Netptune.Core.Requests.Pages;

public sealed record MovePageRequest
{
    public int? ParentId { get; init; }

    public double SortOrder { get; init; }
}
