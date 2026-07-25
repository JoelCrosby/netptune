using Mediator;

using Netptune.Core.Entities;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Services.Automations;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.Automations;

namespace Netptune.Handlers.Automations.Commands;

public sealed record CloneAutomationRuleCommand(int Id, AutomationCloneRequest Request)
    : IRequest<ClientResponse<AutomationRuleViewModel>>;

public sealed class CloneAutomationRuleCommandHandler
    : IRequestHandler<CloneAutomationRuleCommand, ClientResponse<AutomationRuleViewModel>>
{
    private const int MaximumNameLength = 256;

    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAutomationActionRegistry ActionRegistry;

    public CloneAutomationRuleCommandHandler(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAutomationActionRegistry actionRegistry)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        ActionRegistry = actionRegistry;
    }

    public async ValueTask<ClientResponse<AutomationRuleViewModel>> Handle(
        CloneAutomationRuleCommand request,
        CancellationToken cancellationToken)
    {
        var requestedName = request.Request.Name?.Trim();
        var hasRequestedName = !string.IsNullOrWhiteSpace(requestedName);

        if (hasRequestedName && requestedName!.Length > MaximumNameLength)
        {
            return ClientResponse<AutomationRuleViewModel>.Failed(
                $"Automation names cannot be longer than {MaximumNameLength} characters.");
        }

        var workspaceId = await Identity.GetWorkspaceId();
        var source = await UnitOfWork.Automations.GetRuleInWorkspace(
            request.Id,
            workspaceId,
            true,
            cancellationToken);

        if (source is null)
        {
            return ClientResponse<AutomationRuleViewModel>.NotFound;
        }

        var userId = Identity.GetCurrentUserId();
        var clone = new AutomationRule
        {
            WorkspaceId = workspaceId,
            Name = hasRequestedName ? requestedName! : BuildCloneName(source.Name),
            IsEnabled = false,
            ExecutionUserId = source.ExecutionUserId,
            ProjectId = source.ProjectId,
            BoardId = source.BoardId,
            SprintId = source.SprintId,
            TriggerType = source.TriggerType,
            TriggerConfig = source.TriggerConfig,
            OwnerId = userId,
            CreatedByUserId = userId,
            Actions = source.Actions
                .OrderBy(action => action.SortOrder)
                .ThenBy(action => action.Id)
                .Select((action, index) => new AutomationAction
                {
                    Type = action.Type,
                    SortOrder = index,
                    Config = action.Config,
                    OwnerId = userId,
                    CreatedByUserId = userId,
                })
                .ToList(),
        };

        await UnitOfWork.Automations.AddAsync(clone, cancellationToken);
        await UnitOfWork.CompleteAsync(cancellationToken);

        var viewModel = clone.ToViewModel(ActionRegistry);

        return ClientResponse<AutomationRuleViewModel>.Success(viewModel);
    }

    private static string BuildCloneName(string name)
    {
        const string suffix = " (copy)";
        var trimmedName = name.Trim();
        var fitsWithSuffix = trimmedName.Length + suffix.Length <= MaximumNameLength;

        if (fitsWithSuffix)
        {
            return $"{trimmedName}{suffix}";
        }

        var availableLength = MaximumNameLength - suffix.Length;

        return $"{trimmedName[..availableLength]}{suffix}";
    }
}
