namespace Netptune.Core.Models.Activity;

public sealed record FileActivityMeta
{
    public int WorkspaceFileId { get; init; }

    public string FileName { get; init; } = null!;

    public long SizeBytes { get; init; }

    public string ContentType { get; init; } = null!;

    public string UploaderUserId { get; init; } = null!;
}
