namespace Netptune.Core.Storage;

public sealed record StorageUploadOptions
{
    public required string Name { get; init; }

    public required string Key { get; init; }

    public string ContentType { get; init; } = "application/octet-stream";

    public StorageAccess Access { get; init; } = StorageAccess.Private;
}
