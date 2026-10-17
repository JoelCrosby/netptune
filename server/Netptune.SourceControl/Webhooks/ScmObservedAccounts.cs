using Netptune.Core.SourceControl.Events;
using Netptune.Core.SourceControl.Models;

namespace Netptune.SourceControl.Webhooks;

internal static class ScmObservedAccounts
{
    public static IEnumerable<ScmIdentity> Of(ScmEvent scmEvent)
    {
        var authors = scmEvent switch
        {
            ScmPullRequestChanged changed => [changed.PullRequest.Author],
            ScmCommitsPushed pushed => pushed.Commits.Select(commit => commit.Author),
            _ => Enumerable.Empty<ScmIdentity>(),
        };

        var accounts = scmEvent.Actor is null ? authors : authors.Append(scmEvent.Actor);

        return accounts.Where(account => account.ExternalId is not null);
    }
}
