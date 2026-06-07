using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations.Commands;

public sealed record DeleteAutomationRuleCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteAutomationRuleCommandHandler : IRequestHandler<DeleteAutomationRuleCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public DeleteAutomationRuleCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(DeleteAutomationRuleCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (rule is null) return ClientResponse.NotFound;

        rule.IsEnabled = false;
        rule.Delete(userId);

        foreach (var action in rule.Actions)
        {
            action.Delete(userId);
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
