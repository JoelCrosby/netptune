using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record StopAiChangeSetApplyCommand(Guid ChangeSetId) : IRequest<ClientResponse>;

public sealed class StopAiChangeSetApplyCommandHandler : IRequestHandler<StopAiChangeSetApplyCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCancellationRegistry Cancellations;

    public StopAiChangeSetApplyCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCancellationRegistry cancellations)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Cancellations = cancellations;
    }

    public async ValueTask<ClientResponse> Handle(
        StopAiChangeSetApplyCommand command,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var changeSet = await UnitOfWork.AiChangeSets.GetOwned(
            command.ChangeSetId,
            userId,
            workspaceId,
            cancellationToken);

        if (changeSet is null)
        {
            return ClientResponse.NotFound;
        }

        Cancellations.Stop(command.ChangeSetId);

        return ClientResponse.Success;
    }
}
