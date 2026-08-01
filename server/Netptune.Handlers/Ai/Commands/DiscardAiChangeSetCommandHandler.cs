using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Ai.Commands;

public sealed record DiscardAiChangeSetCommand(Guid ChangeSetId) : IRequest<ClientResponse>;

public sealed class DiscardAiChangeSetCommandHandler : IRequestHandler<DiscardAiChangeSetCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DiscardAiChangeSetCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(
        DiscardAiChangeSetCommand command,
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

        var isPending = changeSet.Status == AiChangeSetStatus.Pending;

        if (!isPending)
        {
            return ClientResponse.Failed("Only a pending change set can be discarded.");
        }

        changeSet.Status = AiChangeSetStatus.Discarded;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
