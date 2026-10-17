using Netptune.Core.Entities;
using Netptune.Core.Repositories.Common;

namespace Netptune.Core.Repositories;

public interface ISourceControlCheckRunRepository : IRepository<SourceControlCheckRun, long>
{
    Task<SourceControlCheckRun?> GetByExternalId(int repoId, string externalId, CancellationToken cancellationToken = default);

    Task<List<SourceControlCheckRun>> GetForCommits(IReadOnlyCollection<int> repoIds, IReadOnlyCollection<string> commitShas, CancellationToken cancellationToken = default);

    Task<int> DeleteUpdatedBefore(DateTime updatedBefore, CancellationToken cancellationToken = default);
}
