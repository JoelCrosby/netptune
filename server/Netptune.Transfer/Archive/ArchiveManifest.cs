namespace Netptune.Transfer.Archive;

public sealed record ArchiveProducer
{
    public string App { get; init; } = "netptune";

    public string Version { get; init; } = "1";
}

public sealed record ArchiveWorkspace
{
    public required string Slug { get; init; }

    public required string Name { get; init; }
}

public sealed record ArchiveScope
{
    public bool IncludeHistory { get; init; }

    public bool IncludeFiles { get; init; }

    public bool IncludeMembers { get; init; }
}

public sealed record ArchiveContent
{
    public required string Type { get; init; }

    public required string File { get; init; }

    public long Count { get; init; }

    public string? Sha256 { get; init; }
}

public sealed record ArchiveManifest
{
    public const int CurrentSchemaVersion = 1;

    public const string FileName = "manifest.json";

    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public ArchiveProducer Producer { get; init; } = new();

    public DateTime CreatedAt { get; init; }

    public required ArchiveWorkspace Workspace { get; init; }

    public required ArchiveScope Scope { get; init; }

    public IReadOnlyList<ArchiveContent> Contents { get; init; } = [];

    public IReadOnlyList<string> Redactions { get; init; } = [];

    public int DisambiguatedRefs { get; init; }

    public long FileBytes { get; init; }
}
