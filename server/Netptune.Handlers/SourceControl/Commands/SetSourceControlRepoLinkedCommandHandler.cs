using Mediator;

using Netptune.Core.Requests.SourceControl;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
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

    public SetSourceControlRepoLinkedCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
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
            var message = $"{repo.FullName} is no longer visible to this connection.";

            return ClientResponse<SourceControlRepoViewModel>.Failed(message);
        }

        var isUnchanged = repo.IsLinked == isLinking;

        if (!isUnchanged)
        {
            repo.IsLinked = isLinking;
            repo.LinkedAt = isLinking ? DateTime.UtcNow : null;
            repo.LinkedByUserId = isLinking ? Identity.GetCurrentUserId() : null;

            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        var viewModel = SourceControlMapping.ToViewModel(repo);

        return ClientResponse<SourceControlRepoViewModel>.Success(viewModel);
    }
}
