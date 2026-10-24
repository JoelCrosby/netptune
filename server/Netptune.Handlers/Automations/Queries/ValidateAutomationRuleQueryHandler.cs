using Mediator;

using Netptune.Automation.Rules;
using Netptune.Core.Cache;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations.Queries;

public sealed record ValidateAutomationRuleQuery(AutomationRuleRequest Request) : IRequest<ClientResponse>;

public sealed class ValidateAutomationRuleQueryHandler : IRequestHandler<ValidateAutomationRuleQuery, ClientResponse>
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;
    private readonly IWorkspacePermissionCache PermissionCache;

    public ValidateAutomationRuleQueryHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry,
        IWorkspacePermissionCache permissionCache)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
        PermissionCache = permissionCache;
    }

    public async ValueTask<ClientResponse> Handle(
        ValidateAutomationRuleQuery request,
        CancellationToken cancellationToken)
    {
        var validationError = AutomationValidation.Validate(request.Request, ActionRegistry);

        if (validationError is not null)
        {
            return ClientResponse.Failed(validationError);
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var userId = Identity.GetCurrentUserId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var validationContext = new AutomationValidationContext
        {
            Request = request.Request,
            WorkspaceId = workspaceId,
            UserId = userId,
            WorkspaceKey = workspaceKey,
            UnitOfWork = UnitOfWork,
            PermissionCache = PermissionCache,
            ActionRegistry = ActionRegistry,
        };
        var principalError = await AutomationPrincipalValidation.Validate(validationContext, cancellationToken);

        if (principalError is not null)
        {
            return ClientResponse.Failed(principalError);
        }

        var referenceError = await AutomationReferenceValidation.Validate(validationContext, cancellationToken);

        if (referenceError is not null)
        {
            return ClientResponse.Failed(referenceError);
        }

        return ClientResponse.Success;
    }
}
