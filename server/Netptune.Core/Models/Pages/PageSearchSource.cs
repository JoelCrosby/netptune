using Netptune.Core.Enums;

namespace Netptune.Core.Models.Pages;

public sealed record PageSearchSource
{
    public int Id { get; init; }

    public int? ParentId { get; init; }

    public PageKind Kind { get; init; }

    public required string Title { get; init; }

    public required string Slug { get; init; }

    public string? Body { get; init; }

    public PageStatus Status { get; init; }

    public int? ProjectId { get; init; }

    public List<string> Tags { get; init; } = [];

    public DateTime UpdatedAt { get; init; }
}
