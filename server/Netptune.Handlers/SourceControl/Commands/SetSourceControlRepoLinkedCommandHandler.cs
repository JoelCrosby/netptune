using Mediator;

using Netptune.Core.Localization;
using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.SourceControl.History;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.SourceControl;

namespace Netptune.Handlers.SourceControl.Commands;

public sealed record SetSourceControlRepoLinkedCommand(int RepoId, SetSourceControlRepoLinkedRequest Request)
    : IRequest<ClientResponse<SourceControlRepoViewModel>>;

public sealed class SetSourceControlRepoLinkedCommandHandler
    : IRequestHandler<SetSourceControlRepoLinkedCommand, ClientResponse<SourceControlRepoViewModel>>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IEventPublisher EventPublisher;

    public SetSourceControlRepoLinkedCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IEventPublisher eventPublisher)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        EventPublisher = eventPublisher;
    }

    public async ValueTask<ClientResponse<SourceControlRepoViewModel>> Handle(
        SetSourceControlRepoLinkedCommand command,
        CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var repo = await UnitOfWork.SourceControlRepos.GetOwned(command.RepoId, workspaceId, cancellationToken);

        if (repo is null)
        {
            return ClientResponse<SourceControlRepoViewModel>.NotFound;
        }

        var isLinking = command.Request.IsLinked;
        var isUnavailable = isLinking && !repo.IsAccessible;

        if (isUnavailable)
        {
            var message = Localized.Format(AppText.ErrorNoLongerVisibleConnection, repo.FullName);

            return ClientResponse<SourceControlRepoViewModel>.Failed(message);
        }

        var isUnchanged = repo.IsLinked == isLinking;

        if (!isUnchanged)
        {
            var now = DateTime.UtcNow;

            repo.IsLinked = isLinking;
            repo.LinkedAt = isLinking ? now : null;
            repo.LinkedByUserId = isLinking ? Identity.GetCurrentUserId() : null;

            if (isLinking)
            {
                repo.RequestHistorySync(now);
            }

            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        var isNewlyLinked = isLinking && !isUnchanged;

        if (isNewlyLinked)
        {
            await EventPublisher.Dispatch(new ScmHistorySyncMessage { RepoId = repo.Id });
        }

        var viewModel = SourceControlMapping.ToViewModel(repo);

        return ClientResponse<SourceControlRepoViewModel>.Success(viewModel);
    }
}
