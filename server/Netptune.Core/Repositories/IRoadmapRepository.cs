using Netptune.Core.Models.Reporting;
using Netptune.Core.Models.Roadmap;
using Netptune.Core.ViewModels.Roadmap;

namespace Netptune.Core.Repositories;

public interface IRoadmapRepository
{
    Task<RoadmapViewModel> GetRoadmap(ReportingScope scope, RoadmapFilter filter, CancellationToken cancellationToken = default);
}
