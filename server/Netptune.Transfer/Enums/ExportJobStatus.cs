using System.Collections.Frozen;

namespace Netptune.Transfer.Enums;

public enum ExportJobStatus
{
    Pending = 0,
    Running = 1,
    Succeeded = 2,
    Failed = 3,
    Cancelled = 4,
    Expired = 5,
}

public static class ExportJobStatuses
{
    private static readonly FrozenSet<ExportJobStatus> InFlight = new[]
    {
        ExportJobStatus.Pending,
        ExportJobStatus.Running,
    }.ToFrozenSet();

    public static bool CanRun(ExportJobStatus status) => status is ExportJobStatus.Pending;

    public static bool CanCancel(ExportJobStatus status) => InFlight.Contains(status);

    public static bool HasArtefact(ExportJobStatus status) => status is ExportJobStatus.Succeeded;
}
