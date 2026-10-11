using Netptune.Core.Enums;

namespace Netptune.Core.Requests.Pages;

public sealed record CreatePageRequest
{
    public PageKind Kind { get; init; } = PageKind.Document;

    public int? ParentId { get; init; }

    public int? ProjectId { get; init; }

    public string? Title { get; init; }

    public string? Body { get; init; }

    public string? TemplateKey { get; init; }

    public int? SprintId { get; init; }
}
