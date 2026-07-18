using Netptune.Core.Models.Reporting;

namespace Netptune.Core.Repositories;

public interface IReportingRepository
{
    Task<FlowReport> GetFlow(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken = default);

    Task<WorkloadReport> GetWorkload(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken = default);

    Task<SprintBurndownReport?> GetBurndown(ReportingScope scope, int sprintId, ReportingUnit unit, string timeZone, CancellationToken cancellationToken = default);

    Task<VelocityReport> GetVelocity(ReportingScope scope, int projectId, ReportingUnit unit, int take, CancellationToken cancellationToken = default);
}
