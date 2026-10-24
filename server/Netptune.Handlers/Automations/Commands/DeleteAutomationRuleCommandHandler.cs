using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Activity;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations.Commands;

public sealed record DeleteAutomationRuleCommand(int Id) : IRequest<ClientResponse>;

public sealed class DeleteAutomationRuleCommandHandler : IRequestHandler<DeleteAutomationRuleCommand, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IActivityLogger Activity;

    public DeleteAutomationRuleCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IActivityLogger activity)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Activity = activity;
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

        var cancelledCount = await UnitOfWork.Transaction(async () =>
        {
            var cancelled = await UnitOfWork.Automations.CancelPendingRuleActions(rule.Id, userId, cancellationToken);

            await UnitOfWork.CompleteAsync(cancellationToken);

            return cancelled;
        });

        Activity.LogWith<AutomationRuleActivityMeta>(options =>
        {
            options.EntityId = rule.Id;
            options.EntityType = EntityType.AutomationRule;
            options.Type = ActivityType.Delete;
            options.Meta = new AutomationRuleActivityMeta
            {
                Name = rule.Name,
                CancelledScheduledActions = AutomationRuleActivityMeta.CancelledCount(cancelledCount),
            };
        });

        return ClientResponse.Success;
    }
}
