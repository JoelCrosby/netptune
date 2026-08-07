using Netptune.Transfer.Enums;

namespace Netptune.Transfer.Services;

public sealed record ExportJobProgressEvent
{
    public required Guid PublicId { get; init; }

    public required ExportJobStatus Status { get; init; }

    public required int ProgressPercent { get; init; }

    public string? ProgressMessage { get; init; }

    public string? Error { get; init; }
}

public sealed record ImportSessionProgressEvent
{
    public required Guid PublicId { get; init; }

    public required ImportStage Stage { get; init; }

    public required int ProgressPercent { get; init; }

    public string? ProgressMessage { get; init; }

    public string? Error { get; init; }
}

public static class TransferJobEventNames
{
    public const string ExportProgress = "export-job-progress";
    public const string ImportProgress = "import-session-progress";

    public static bool IsKnown(string? name)
    {
        return name is ExportProgress or ImportProgress;
    }
}

public interface ITransferJobNotifier
{
    Task PublishExportAsync(string workspaceSlug, ExportJobProgressEvent progressEvent, CancellationToken cancellationToken = default);

    Task PublishImportAsync(string workspaceSlug, ImportSessionProgressEvent progressEvent, CancellationToken cancellationToken = default);
}
