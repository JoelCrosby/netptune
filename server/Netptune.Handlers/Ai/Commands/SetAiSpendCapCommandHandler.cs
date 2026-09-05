using Mediator;

using Netptune.Core.Requests.Ai;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Ai;

namespace Netptune.Handlers.Ai.Commands;

public sealed record SetAiSpendCapCommand(SetAiSpendCapRequest Request) : IRequest<ClientResponse<AiSpendViewModel>>;

public sealed class SetAiSpendCapCommandHandler
    : IRequestHandler<SetAiSpendCapCommand, ClientResponse<AiSpendViewModel>>
{
    private const decimal MaximumCap = 1_000_000m;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiSpendService Spend;

    public SetAiSpendCapCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiSpendService spend)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Spend = spend;
    }

    public async ValueTask<ClientResponse<AiSpendViewModel>> Handle(
        SetAiSpendCapCommand command,
        CancellationToken cancellationToken)
    {
        var cap = command.Request.Cap;
        var isOutOfRange = cap.HasValue && (cap.Value <= 0m || cap.Value > MaximumCap);

        if (isOutOfRange)
        {
            return ClientResponse<AiSpendViewModel>.Failed(
                $"A spend cap must be more than 0 and no more than {MaximumCap:C0}.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var workspace = await UnitOfWork.Workspaces.GetAsync(workspaceId, cancellationToken: cancellationToken);

        if (workspace is null)
        {
            return ClientResponse<AiSpendViewModel>.NotFound;
        }

        workspace.AssistantSpendCap = cap.HasValue ? decimal.Round(cap.Value, 2, MidpointRounding.AwayFromZero) : null;

        await UnitOfWork.CompleteAsync(cancellationToken);

        var summary = await Spend.GetSummary(workspace, cancellationToken);

        return ClientResponse<AiSpendViewModel>.Success(summary);
    }
}
