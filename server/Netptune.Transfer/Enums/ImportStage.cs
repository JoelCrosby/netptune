using System.Collections.Frozen;

namespace Netptune.Transfer.Enums;

public enum ImportStage
{
    Uploaded = 0,
    Inspected = 1,
    Mapped = 2,
    Previewed = 3,
    Committing = 4,
    Committed = 5,
    Failed = 6,
    Undone = 7,
    Abandoned = 8,
}

public static class ImportStages
{
    private static readonly FrozenSet<ImportStage> SourceReadable = new[]
    {
        ImportStage.Uploaded,
        ImportStage.Inspected,
        ImportStage.Mapped,
        ImportStage.Previewed,
        ImportStage.Failed,
    }.ToFrozenSet();

    private static readonly FrozenSet<ImportStage> Profiled = new[]
    {
        ImportStage.Inspected,
        ImportStage.Mapped,
        ImportStage.Previewed,
        ImportStage.Failed,
    }.ToFrozenSet();

    private static readonly FrozenSet<ImportStage> MappingReady = new[]
    {
        ImportStage.Mapped,
        ImportStage.Previewed,
        ImportStage.Failed,
    }.ToFrozenSet();

    public static bool CanInspect(ImportStage stage) => SourceReadable.Contains(stage);

    public static bool CanMap(ImportStage stage) => Profiled.Contains(stage);

    public static bool CanPreview(ImportStage stage) => MappingReady.Contains(stage);

    public static bool CanCommit(ImportStage stage) => MappingReady.Contains(stage);

    public static bool CanRun(ImportStage stage) => stage is ImportStage.Committing;

    public static bool CanUndo(ImportStage stage) => stage is ImportStage.Committed;

    public static bool CanDelete(ImportStage stage) => stage is not ImportStage.Committing;
}
