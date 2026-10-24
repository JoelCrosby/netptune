using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations.Commands;

public sealed record SetAutomationRuleEnabledCommand(int Id, bool IsEnabled) : IRequest<ClientResponse>;

public sealed class SetAutomationRuleEnabledCommandHandler : IRequestHandler<SetAutomationRuleEnabledCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public SetAutomationRuleEnabledCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
    }

    public async ValueTask<ClientResponse> Handle(SetAutomationRuleEnabledCommand request, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var rule = await UnitOfWork.Automations.GetRuleInWorkspace(request.Id, workspaceId, cancellationToken: cancellationToken);

        if (rule is null) return ClientResponse.NotFound;

        var isUnchanged = rule.IsEnabled == request.IsEnabled;

        if (isUnchanged)
        {
            return ClientResponse.Success;
        }

        var userId = Identity.GetCurrentUserId();

        rule.IsEnabled = request.IsEnabled;

        if (request.IsEnabled)
        {
            rule.AutoDisabledAt = null;
            rule.AutoDisabledReason = null;
            rule.AutoDisabledReasonCode = null;
            rule.AutoDisabledReasonArguments = [];
        }

        rule.ModifiedByUserId = userId;

        var cancelledCount = await UnitOfWork.Transaction(async () =>
        {
            var cancelled = request.IsEnabled
                ? 0
                : await UnitOfWork.Automations.CancelPendingRuleActions(rule.Id, userId, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return cancelled;
        });

        Activity.LogWith<AutomationRuleActivityMeta>(options =>
        {
            options.EntityId = rule.Id;
            options.EntityType = EntityType.AutomationRule;
            options.Type = request.IsEnabled ? ActivityType.Enable : ActivityType.Disable;
            options.Meta = new AutomationRuleActivityMeta
            {
                Name = rule.Name,
                CancelledScheduledActions = AutomationRuleActivityMeta.CancelledCount(cancelledCount),
            };
        });

        return ClientResponse.Success;
    }
}
