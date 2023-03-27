using System;

namespace Netptune.Core.Models.Import;

public record TaskImportRow
{
    public required string Name { get; init; }

    public string? Description { get; init; }

    public string? Status { get; init; }

    public string? IsFlagged { get; init; }

    public required  DateTime CreatedAt { get; init; }

    public DateTime? UpdatedAt { get; init; }

    public string? Assignees { get; init; }

    public required  string Owner { get; init; }

    public string? Group { get; init; }

    public string? Tags { get; init; }
}
