namespace Netptune.Transfer.Archive;

public sealed record ArchiveSchemaUpgradeResult
{
    public required ArchiveManifest Manifest { get; init; }

    public int FromVersion { get; init; }

    public int ToVersion { get; init; }

    public bool WasUpgraded => FromVersion != ToVersion;

    public IReadOnlyList<string> Applied { get; init; } = [];
}

public sealed class ArchiveSchemaException(string message, Exception? innerException = null) : Exception(message, innerException);

public static class ArchiveSchemaUpgrader
{
    public static ArchiveSchemaUpgradeResult Upgrade(ArchiveManifest manifest)
    {
        var from = manifest.SchemaVersion;

        if (from > ArchiveManifest.CurrentSchemaVersion)
        {
            throw new ArchiveSchemaException(
                $"This archive was written by a newer version of Netptune (schema {from}, this build understands {ArchiveManifest.CurrentSchemaVersion}). Upgrade before importing it.");
        }

        if (from < 1)
        {
            throw new ArchiveSchemaException($"Schema version {from} is not a version this build recognises.");
        }

        var applied = new List<string>();
        var upgraded = manifest;

        while (upgraded.SchemaVersion < ArchiveManifest.CurrentSchemaVersion)
        {
            var step = upgraded.SchemaVersion;

            upgraded = Step(upgraded, step);
            applied.Add($"{step} → {upgraded.SchemaVersion}");
        }

        return new ArchiveSchemaUpgradeResult
        {
            Manifest = upgraded,
            FromVersion = from,
            ToVersion = upgraded.SchemaVersion,
            Applied = applied,
        };
    }

    private static ArchiveManifest Step(ArchiveManifest _, int fromVersion)
    {
        return fromVersion switch
        {
            _ => throw new ArchiveSchemaException($"No upgrade step is defined for schema version {fromVersion}."),
        };
    }
}
