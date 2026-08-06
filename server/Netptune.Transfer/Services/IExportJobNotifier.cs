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

public interface IExportJobNotifier
{
    Task PublishAsync(string workspaceSlug, ExportJobProgressEvent progressEvent, CancellationToken cancellationToken = default);
}
