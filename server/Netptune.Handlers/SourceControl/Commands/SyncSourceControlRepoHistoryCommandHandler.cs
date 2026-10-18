using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.SourceControl;
using Netptune.Core.SourceControl.History;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record SyncSourceControlRepoHistoryCommand(int RepoId)
    : IRequest<ClientResponse<SourceControlRepoViewModel>>;

public sealed class SyncSourceControlRepoHistoryCommandHandler
    : IRequestHandler<SyncSourceControlRepoHistoryCommand, ClientResponse<SourceControlRepoViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventPublisher EventPublisher;

    public SyncSourceControlRepoHistoryCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse<SourceControlRepoViewModel>> Handle(
        SyncSourceControlRepoHistoryCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var repo = await UnitOfWork.SourceControlRepos.GetOwned(command.RepoId, workspaceId, cancellationToken);

        if (repo is null)
        {
            return ClientResponse<SourceControlRepoViewModel>.NotFound;
        }

        var isTracked = repo is { IsLinked: true, IsAccessible: true };

        if (!isTracked)
        {
            var message = $"Link {repo.FullName} before syncing its history.";

            return ClientResponse<SourceControlRepoViewModel>.Failed(message);
        }

        var now = DateTime.UtcNow;
        var isAlreadySyncing = ScmHistorySyncState.Of(repo, now) == SourceControlHistorySyncStatus.Syncing;

        if (!isAlreadySyncing)
        {
            repo.RequestHistorySync(now);

            await UnitOfWork.CompleteAsync(cancellationToken);
            await EventPublisher.Dispatch(new ScmHistorySyncMessage { RepoId = repo.Id });
        }

        var viewModel = SourceControlMapping.ToViewModel(repo);

        return ClientResponse<SourceControlRepoViewModel>.Success(viewModel);
    }
}
