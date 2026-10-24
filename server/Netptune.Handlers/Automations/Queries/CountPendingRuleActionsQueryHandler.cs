using Mediator;

using Netptune.Core.Services;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations.Queries;

public sealed record CountPendingRuleActionsQuery(int RuleId) : IRequest<int>;

public sealed class CountPendingRuleActionsQueryHandler : IRequestHandler<CountPendingRuleActionsQuery, int>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public CountPendingRuleActionsQueryHandler(INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<int> Handle(CountPendingRuleActionsQuery request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(
            request.RuleId,
            workspaceId,
            true,
            cancellationToken);

        if (rule is null)
        {
            return 0;
        }

        return await UnitOfWork.Automations.CountPendingRuleActions(rule.Id, cancellationToken);
    }
}
