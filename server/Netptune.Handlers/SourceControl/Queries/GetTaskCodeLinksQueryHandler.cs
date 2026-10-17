using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Queries;

public sealed record GetTaskCodeLinksQuery(string SystemId) : IRequest<ClientResponse<List<TaskCodeLinkViewModel>>>;

public sealed class GetTaskCodeLinksQueryHandler
    : IRequestHandler<GetTaskCodeLinksQuery, ClientResponse<List<TaskCodeLinkViewModel>>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public GetTaskCodeLinksQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse<List<TaskCodeLinkViewModel>>> Handle(
        GetTaskCodeLinksQuery query,
        CancellationToken cancellationToken)
    {
        var workspaceKey = Identity.GetWorkspaceKey();
        var workspaceId = await Identity.GetWorkspaceId();
        var taskIds = await UnitOfWork.Tasks.GetTaskIdsBySystemIds(
            [query.SystemId],
            workspaceKey,
            includeArchived: true,
            cancellationToken);

        if (taskIds.Count == 0)
        {
            return ClientResponse<List<TaskCodeLinkViewModel>>.NotFound;
        }

        var taskId = taskIds.Values.First();
        var links = await UnitOfWork.TaskCodeLinks.GetForTask(taskId, workspaceId, cancellationToken);
        var checkRuns = await GetCheckRuns(links, cancellationToken);

        var viewModels = links
            .Select(link => ToViewModel(link, checkRuns))
            .ToList();

        return ClientResponse<List<TaskCodeLinkViewModel>>.Success(viewModels);
    }

    private async Task<ILookup<(int RepoId, string Sha), SourceControlCheckRun>> GetCheckRuns(
        IReadOnlyCollection<TaskCodeLink> links,
        CancellationToken cancellationToken)
    {
        var commitShas = links
            .Select(CommitShaOf)
            .OfType<string>()
            .Distinct()
            .ToList();

        if (commitShas.Count == 0)
        {
            return Array.Empty<SourceControlCheckRun>().ToLookup(checkRun => (checkRun.RepoId, checkRun.CommitSha));
        }

        var repoIds = links.Select(link => link.RepoId).Distinct().ToList();
        var checkRuns = await UnitOfWork.SourceControlCheckRuns.GetForCommits(repoIds, commitShas, cancellationToken);

        return checkRuns.ToLookup(checkRun => (checkRun.RepoId, checkRun.CommitSha));
    }

    private static string? CommitShaOf(TaskCodeLink link)
    {
        return link.Kind switch
        {
            TaskCodeLinkKind.Commit => link.ExternalRef,
            TaskCodeLinkKind.PullRequest => link.HeadSha,
            _ => null,
        };
    }

    private static TaskCodeLinkViewModel ToViewModel(
        TaskCodeLink link,
        ILookup<(int RepoId, string Sha), SourceControlCheckRun> checkRuns)
    {
        var commitSha = CommitShaOf(link);
        var checks = commitSha is null ? null : ScmCheckSummary.Summarise(checkRuns[(link.RepoId, commitSha)]);

        return new TaskCodeLinkViewModel
        {
            Id = link.Id,
            Kind = link.Kind,
            ExternalRef = link.ExternalRef,
            Title = link.Title,
            WebUrl = link.WebUrl,
            AuthorLogin = link.AuthorLogin,
            PullRequestState = link.PullRequestState,
            IsRemoved = link.IsRemoved,
            OccurredAt = link.OccurredAt,
            Provider = link.Repo.Connection.Provider,
            RepoFullName = link.Repo.FullName,
            RepoWebUrl = link.Repo.WebUrl,
            Checks = checks,
        };
    }
}
