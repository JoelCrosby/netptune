using Netptune.Core.Cache;
using Netptune.Core.Requests;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Automations;

internal sealed record AutomationValidationContext
{
    public required AutomationRuleRequest Request { get; init; }

    public required int WorkspaceId { get; init; }

    public required string UserId { get; init; }

    public required string WorkspaceKey { get; init; }

    public required INetptuneUnitOfWork UnitOfWork { get; init; }

    public required IWorkspacePermissionCache PermissionCache { get; init; }

    public required IAutomationActionRegistry ActionRegistry { get; init; }
}
