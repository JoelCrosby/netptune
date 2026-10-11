using Netptune.Core.Enums;

namespace Netptune.Core.Requests.Pages;

public sealed record CreatePageRequest
{
    public PageKind Kind { get; init; } = PageKind.Document;

    public int? ParentId { get; init; }

    public string? Title { get; init; }
}
