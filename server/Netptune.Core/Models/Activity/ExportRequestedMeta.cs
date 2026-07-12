namespace Netptune.Core.Models.Activity;

public record ExportRequestedMeta
{
    public required string ExportType { get; init; }

    public string? Scope { get; init; }
}
