using Netptune.Core.Models.Reporting;

namespace Netptune.Core.Services.Reporting;

public interface IReportingScopeResolver
{
    Task<ReportingScope?> Resolve(CancellationToken cancellationToken = default);
}
