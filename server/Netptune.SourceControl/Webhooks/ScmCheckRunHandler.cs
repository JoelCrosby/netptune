using Netptune.Core.Entities;
using Netptune.Core.Events;
using Netptune.Core.Services.Realtime;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.SourceControl.Webhooks;

internal sealed class ScmCheckRunHandler : IScmEventHandler
{
    private const int MaxNameLength = 256;
    private const int ShortShaLength = 7;
    private const int MaxExternalIdLength = 128;
    private const int MaxCommitShaLength = 64;
    private const int MaxWebUrlLength = 1024;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ScmTaskActivityRecorder Activity;
    private readonly TimeProvider Clock;

    public ScmCheckRunHandler(INetptuneUnitOfWork unitOfWork, ScmTaskActivityRecorder activity, TimeProvider clock)
    {
        UnitOfWork = unitOfWork;
        Activity = activity;
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
        var commitRuns = await UnitOfWork.SourceControlCheckRuns.GetForCommits(
            [repo.Id],
            [checkRun.CommitSha],
            cancellationToken);

        SourceControlCheckRun current;

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

            current = created;
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

            current = stored;
        }

        var runsAfter = commitRuns
            .Where(run => run.ExternalId != current.ExternalId)
            .Append(current)
            .ToList();
        var summaryAfter = ScmCheckSummary.Summarise(runsAfter)!;
        var links = await UnitOfWork.TaskCodeLinks.GetReferencingCommit(repo.Id, checkRun.CommitSha, cancellationToken);

        await RecordOutcome(summaryAfter, links, repo, connection, changed, cancellationToken);

        return links.Count > 0;
    }

    // Each link remembers the last result recorded for its commit, so ten checks produce one entry, and a
    // job created after the others passed (a workflow with dependent jobs) doesn't record "passed" again.
    private async Task RecordOutcome(
        ScmCheckSummaryViewModel summaryAfter,
        List<TaskCodeLink> links,
        SourceControlRepo repo,
        SourceControlConnection connection,
        ScmCheckRunChanged changed,
        CancellationToken cancellationToken)
    {
        var eventKey = summaryAfter.State switch
        {
            ScmCheckState.Success => EventKeys.SourceControlChecksPassed,
            ScmCheckState.Failure => EventKeys.SourceControlChecksFailed,
            _ => null,
        };

        if (eventKey is null)
        {
            return;
        }

        var unrecordedLinks = links.Where(link => link.LastCheckState != summaryAfter.State).ToList();

        foreach (var link in unrecordedLinks)
        {
            link.LastCheckState = summaryAfter.State;
        }

        var subjectsByTask = unrecordedLinks
            .GroupBy(link => link.TaskId)
            .Select(group => group.FirstOrDefault(link => link.Kind == TaskCodeLinkKind.PullRequest) ?? group.First())
            .GroupBy(link => (link.Kind, link.ExternalRef))
            .ToList();

        foreach (var subject in subjectsByTask)
        {
            var link = subject.First();

            await Activity.Record(new ScmTaskActivity
            {
                EventKey = eventKey!,
                TaskIds = subject.Select(task => task.TaskId).ToList(),
                Repo = repo,
                Provider = connection.Provider,
                Reference = ReferenceOf(link),
                Title = link.Title,
                WebUrl = summaryAfter.AttentionUrl ?? link.WebUrl,
                OccurredAt = changed.OccurredAt,
                Checks = new ScmCheckSummaryCounts(summaryAfter.Total, summaryAfter.Failed),
            }, cancellationToken);
        }
    }

    private static string ReferenceOf(TaskCodeLink link)
    {
        var isCommit = link.Kind == TaskCodeLinkKind.Commit;
        var shortSha = link.ExternalRef[..Math.Min(ShortShaLength, link.ExternalRef.Length)];

        return isCommit ? shortSha : $"#{link.ExternalRef}";
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
