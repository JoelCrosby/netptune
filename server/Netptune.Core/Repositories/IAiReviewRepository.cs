using Netptune.Core.Entities;
using Netptune.Core.Models.Ai;
using Netptune.Core.Repositories.Common;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Core.Repositories;

public interface IAiReviewRepository : IRepository<AiReviewRun, int>
{
    Task<List<AiReviewRunViewModel>> GetRuns(AiReviewSubject subject, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiReviewRunViewModel?> GetRun(int runId, AiReviewSubject subject, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<AiReviewFindingViewModel>> GetFindings(int runId, CancellationToken cancellationToken = default);

    Task<List<AiReviewDismissal>> GetDismissals(AiReviewSubject subject, int workspaceId, CancellationToken cancellationToken = default);

    Task<List<AiReviewMentionViewModel>> GetMentions(AiReviewSubject subject, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiReviewOriginViewModel?> GetOrigin(AiReviewSubject created, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiReviewRun?> GetRunForUpdate(int runId, int workspaceId, CancellationToken cancellationToken = default);

    Task<AiReviewFinding?> GetFindingForUpdate(int runId, long findingId, CancellationToken cancellationToken = default);

    Task<bool> HasNewerSucceededRun(AiReviewRun run, CancellationToken cancellationToken = default);

    Task<Dictionary<int, string>> GetTaskSpecFingerprints(int workspaceId, IReadOnlyCollection<int> taskIds, CancellationToken cancellationToken = default);
}
