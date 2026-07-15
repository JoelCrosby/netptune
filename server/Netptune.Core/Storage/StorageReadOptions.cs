namespace Netptune.Core.Storage;

public sealed record StorageReadOptions
{
    public required string Key { get; init; }

    public required string FileName { get; init; }

    public required StorageDisposition Disposition { get; init; }

    public required TimeSpan Lifetime { get; init; }
}
