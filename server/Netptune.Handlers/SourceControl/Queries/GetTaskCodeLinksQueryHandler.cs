using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Entities;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;
using Netptune.Core.ViewModels.Users;

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
        var authors = await GetAuthorMatcher(links, workspaceId, cancellationToken);

        var viewModels = links
            .Select(link => ToViewModel(link, checkRuns, authors))
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

    private async Task<ScmAuthorMatcher> GetAuthorMatcher(
        List<TaskCodeLink> links,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var identities = new List<UserSourceControlIdentity>();

        // One lookup per provider present on the task, so at most one per supported provider.
        foreach (var providerLinks in links.GroupBy(link => link.Repo.Connection.Provider))
        {
            var externalIds = providerLinks
                .Select(link => link.AuthorExternalId)
                .OfType<string>()
                .Distinct()
                .ToList();
            var usernames = providerLinks
                .Select(link => link.AuthorLogin)
                .OfType<string>()
                .Select(UserSourceControlIdentity.Normalize)
                .Distinct()
                .ToList();

            var providerIdentities = await UnitOfWork.UserSourceControlIdentities.GetMemberIdentities(
                workspaceId,
                providerLinks.Key,
                externalIds,
                usernames,
                cancellationToken);

            identities.AddRange(providerIdentities);
        }

        var emails = links
            .Select(link => link.AuthorEmail)
            .OfType<string>()
            .Select(ScmAuthorMatcher.NormalizeEmail)
            .Distinct()
            .ToList();

        var membersByEmail = emails.Count == 0
            ? []
            : await UnitOfWork.UserSourceControlIdentities.GetMembersByEmail(workspaceId, emails, cancellationToken);

        return new ScmAuthorMatcher(identities, membersByEmail);
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
        ILookup<(int RepoId, string Sha), SourceControlCheckRun> checkRuns,
        ScmAuthorMatcher authors)
    {
        var commitSha = CommitShaOf(link);
        var checks = commitSha is null ? null : ScmCheckSummary.Summarise(checkRuns[(link.RepoId, commitSha)]);
        var provider = link.Repo.Connection.Provider;
        var author = authors.Match(provider, link.AuthorExternalId, link.AuthorLogin, link.AuthorEmail);

        return new TaskCodeLinkViewModel
        {
            Id = link.Id,
            Kind = link.Kind,
            ExternalRef = link.ExternalRef,
            Title = link.Title,
            WebUrl = link.WebUrl,
            AuthorLogin = link.AuthorLogin,
            Author = author is null ? null : ToAuthor(author),
            PullRequestState = link.PullRequestState,
            IsRemoved = link.IsRemoved,
            OccurredAt = link.OccurredAt,
            Provider = provider,
            RepoFullName = link.Repo.FullName,
            RepoWebUrl = link.Repo.WebUrl,
            Checks = checks,
        };
    }

    private static AssigneeViewModel ToAuthor(AppUser user)
    {
        return new AssigneeViewModel
        {
            Id = user.Id,
            DisplayName = user.DisplayName,
            PictureUrl = user.PictureUrl,
            IsServiceAccount = user.UserType == AppUserType.ServiceAccount,
        };
    }
}
