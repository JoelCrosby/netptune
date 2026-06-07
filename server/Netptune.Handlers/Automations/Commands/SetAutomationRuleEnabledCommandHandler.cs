using Mediator;

using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations.Commands;

public sealed record SetAutomationRuleEnabledCommand(int Id, bool IsEnabled) : IRequest<ClientResponse>;

public sealed class SetAutomationRuleEnabledCommandHandler : IRequestHandler<SetAutomationRuleEnabledCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public SetAutomationRuleEnabledCommandHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<ClientResponse> Handle(SetAutomationRuleEnabledCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (rule is null) return ClientResponse.NotFound;

        rule.IsEnabled = request.IsEnabled;
        rule.ModifiedByUserId = Identity.GetCurrentUserId();

        await UnitOfWork.CompleteAsync(cancellationToken);

        return ClientResponse.Success;
    }
}
