using Netptune.Core.Entities;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.UnitOfWork;

namespace Netptune.SourceControl.Webhooks;

internal sealed class ScmCheckRunHandler : IScmEventHandler
{
    private const int MaxNameLength = 256;
    private const int MaxExternalIdLength = 128;
    private const int MaxCommitShaLength = 64;
    private const int MaxWebUrlLength = 1024;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly TimeProvider Clock;

    public ScmCheckRunHandler(INetptuneUnitOfWork unitOfWork, TimeProvider clock)
    {
        UnitOfWork = unitOfWork;
        Clock = clock;
    }

    public string RefreshScope => WorkspaceEventScopes.CodeLinks;

    public bool CanHandle(ScmEvent scmEvent)
    {
        return scmEvent is ScmCheckRunChanged;
    }

    public async Task<bool> Handle(
        ScmEvent scmEvent,
        SourceControlConnection connection,
        CancellationToken cancellationToken)
    {
        var changed = (ScmCheckRunChanged)scmEvent;
        var repo = await UnitOfWork.SourceControlRepos.GetByExternalId(
            connection.Id,
            changed.Repository.ExternalId,
            cancellationToken);

        var isTracked = repo is { IsLinked: true, IsAccessible: true };
        var checkRun = changed.CheckRun;
        var isStorable = FitsStorage(checkRun);

        if (!isTracked || !isStorable)
        {
            return false;
        }
        var now = Clock.GetUtcNow().UtcDateTime;
        var stored = await UnitOfWork.SourceControlCheckRuns.GetByExternalId(
            repo!.Id,
            checkRun.ExternalId,
            cancellationToken);

        if (stored is null)
        {
            var created = new SourceControlCheckRun
            {
                WorkspaceId = repo.WorkspaceId,
                RepoId = repo.Id,
                ExternalId = checkRun.ExternalId,
                Name = Truncate(checkRun.Name),
                CommitSha = checkRun.CommitSha,
                WebUrl = checkRun.WebUrl,
            };

            Apply(created, checkRun, now);

            await UnitOfWork.SourceControlCheckRuns.AddAsync(created, cancellationToken);
        }
        else
        {
            // Deliveries can be processed out of order; a late "created" must not undo a finished run.
            var isRegression = stored.Status == ScmCheckRunStatus.Completed
                && checkRun.Status != ScmCheckRunStatus.Completed;

            if (isRegression)
            {
                return false;
            }

            Apply(stored, checkRun, now);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        var isReferenced = await UnitOfWork.TaskCodeLinks.ReferencesCommit(
            repo.Id,
            checkRun.CommitSha,
            cancellationToken);

        return isReferenced;
    }

    private static void Apply(SourceControlCheckRun stored, ScmCheckRun checkRun, DateTime now)
    {
        stored.Name = Truncate(checkRun.Name);
        stored.Status = checkRun.Status;
        stored.Conclusion = checkRun.Conclusion;
        stored.WebUrl = checkRun.WebUrl;
        stored.CompletedAt = checkRun.CompletedAt?.UtcDateTime;
        stored.UpdatedAt = now;
    }

    private static bool FitsStorage(ScmCheckRun checkRun)
    {
        var idFits = checkRun.ExternalId.Length <= MaxExternalIdLength;
        var shaFits = checkRun.CommitSha.Length <= MaxCommitShaLength;
        var urlFits = checkRun.WebUrl.Length <= MaxWebUrlLength;

        return idFits && shaFits && urlFits;
    }

    private static string Truncate(string value)
    {
        return value.Length > MaxNameLength ? value[..MaxNameLength] : value;
    }
}
