using Netptune.Core.Responses.Common;
using Netptune.Core.SourceControl;
using Netptune.Core.UnitOfWork;

namespace Netptune.SourceControl.Repos;

internal sealed class SourceControlRepoSync : ISourceControlRepoSync
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ISourceControlProviderRegistry Registry;
    private readonly TimeProvider Clock;

    public SourceControlRepoSync(
        INetptuneUnitOfWork unitOfWork,
        ISourceControlProviderRegistry registry,
        TimeProvider clock)
    {
        UnitOfWork = unitOfWork;
        Registry = registry;
        Clock = clock;
    }

    public async ValueTask<ClientResponse<ScmRepoSyncResult>> Sync(
        int connectionId,
        int workspaceId,
        CancellationToken cancellationToken)
    {
        var connection = await UnitOfWork.SourceControlConnections.GetOwned(
            connectionId,
            workspaceId,
            cancellationToken);

        if (connection is null)
        {
            return ClientResponse<ScmRepoSyncResult>.NotFound;
        }

        var scmConnection = connection.ToScmConnection();
        var requirement = Registry.Require(scmConnection, SourceControlCapabilities.ListRepositories);

        if (!requirement.IsSuccess)
        {
            return ClientResponse<ScmRepoSyncResult>.Failed(requirement.Message);
        }

        var provider = Registry.Resolve(connection.Provider);
        var context = Registry.CreateContext(scmConnection);
        var remote = await provider.Client.ListRepositories(context, cancellationToken).ToListAsync(cancellationToken);
        var existing = await UnitOfWork.SourceControlRepos.GetForConnection(connection.Id, cancellationToken);
        var existingIds = existing.Select(repo => repo.Id).ToList();
        var reposWithTaskLinks = await UnitOfWork.TaskCodeLinks.GetRepoIdsWithLinks(existingIds, cancellationToken);
        var syncedAt = Clock.GetUtcNow().UtcDateTime;
        var plan = SourceControlRepoSyncPlanner.Plan(connection, existing, remote, reposWithTaskLinks, syncedAt);

        await UnitOfWork.SourceControlRepos.AddRangeAsync(plan.Added, cancellationToken);
        await UnitOfWork.SourceControlRepos.DeletePermanent(plan.Removed, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var result = new ScmRepoSyncResult
        {
            Added = plan.Added.Count,
            Refreshed = plan.Refreshed,
            Removed = plan.Removed.Count,
            MarkedInaccessible = plan.MarkedInaccessible,
        };

        return ClientResponse<ScmRepoSyncResult>.Success(result);
    }
}
