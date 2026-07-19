using Netptune.Core.Models.Reporting;

namespace Netptune.Core.Repositories;

public interface IReportingRepository
{
    Task<FlowReport> GetFlow(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken = default);

    Task<WorkloadReport> GetWorkload(ReportingScope scope, ReportingFilter filter, CancellationToken cancellationToken = default);

    Task<SprintBurndownReport?> GetBurndown(
        ReportingScope scope,
        SprintBurndownFilter filter,
        CancellationToken cancellationToken = default);

    Task<VelocityReport> GetVelocity(
        ReportingScope scope,
        VelocityFilter filter,
        CancellationToken cancellationToken = default);
}
