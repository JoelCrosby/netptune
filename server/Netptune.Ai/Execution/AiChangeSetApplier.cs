using System.Text.Json;

using Mediator;

using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Requests;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;
using Netptune.Handlers.Tasks.Commands;

namespace Netptune.Ai.Execution;

public sealed class AiChangeSetApplier : IAiChangeSetApplier
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IMediator Mediator;
    private readonly IAiToolRegistry Tools;
    private readonly IAiExecutionContext AiExecution;
    private readonly Dictionary<string, IAiChangeHandler> HandlersByToolName;

    public AiChangeSetApplier(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IMediator mediator,
        IAiToolRegistry tools,
        IAiExecutionContext aiExecution,
        IEnumerable<IAiChangeHandler> handlers)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Mediator = mediator;
        Tools = tools;
        AiExecution = aiExecution;
        HandlersByToolName = handlers.ToDictionary(handler => handler.ToolName, StringComparer.Ordinal);
    }

    private async Task<string> ResolveAgentName(AiChangeSet changeSet, CancellationToken cancellationToken)
    {
        var conversation = await UnitOfWork.AiConversations.GetAsync(
            changeSet.ConversationId,
            true,
            cancellationToken);

        return conversation?.Model ?? "assistant";
    }

    public async Task<AiApplyResult?> Apply(
        Guid changeSetId,
        ApplyAiChangeSetRequest request,
        CancellationToken cancellationToken)
    {
        var userId = Identity.GetCurrentUserId();
        var workspaceId = await Identity.GetWorkspaceId();
        var workspaceKey = Identity.GetWorkspaceKey();
        var changeSet = await UnitOfWork.AiChangeSets.GetOwned(changeSetId, userId, workspaceId, cancellationToken);

        if (changeSet is null)
        {
            return null;
        }

        var workspace = await UnitOfWork.Workspaces.GetAsync(workspaceId, true, cancellationToken);
        var isAssistantEnabled = workspace?.AssistantEnabled ?? false;

        if (!isAssistantEnabled)
        {
            throw new InvalidOperationException("The assistant is turned off for this workspace.");
        }

        var isPending = changeSet.Status == AiChangeSetStatus.Pending;

        if (!isPending)
        {
            throw new InvalidOperationException("Only a pending change set can be applied.");
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);
        var selected = SelectChanges(changes, request.ChangeIds);
        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            userId,
            workspaceKey,
            cancellationToken: cancellationToken);

        if (membership is null)
        {
            throw new InvalidOperationException("You are not a member of this workspace.");
        }

        var permissions = membership.Permissions.ToHashSet(StringComparer.Ordinal);
        var missingPermission = selected.Any(change => !HasPermission(change, permissions));

        if (missingPermission)
        {
            throw new UnauthorizedAccessException("You do not have permission to apply these changes.");
        }

        var results = new List<AiAppliedChangeResult>();
        var resolvedRefs = new Dictionary<string, int>(StringComparer.Ordinal);
        var agent = await ResolveAgentName(changeSet, cancellationToken);

        using (AiExecution.Begin(agent, changeSet.CorrelationId))
        {
            foreach (var change in selected)
            {
                var result = await ApplyChange(change, resolvedRefs, cancellationToken);

                results.Add(result);
            }
        }

        MarkUnselected(changes, selected);

        changeSet.Status = ResolveStatus(results);
        changeSet.AppliedAt = DateTime.UtcNow;

        await UnitOfWork.CompleteAsync(cancellationToken);

        return new AiApplyResult
        {
            ChangeSetId = changeSet.Id,
            Status = changeSet.Status,
            Results = results,
        };
    }

    private static List<AiProposedChange> SelectChanges(List<AiProposedChange> changes, List<long> changeIds)
    {
        var isExplicitSelection = changeIds.Count > 0;

        if (!isExplicitSelection)
        {
            return changes.Where(change => change.ValidationStatus == AiChangeValidationStatus.Valid).ToList();
        }

        var selectedIds = changeIds.ToHashSet();

        return changes
            .Where(change => selectedIds.Contains(change.Id))
            .Where(change => change.ValidationStatus == AiChangeValidationStatus.Valid)
            .ToList();
    }

    private bool HasPermission(AiProposedChange change, IReadOnlySet<string> permissions)
    {
        var tool = Tools.Find(change.ToolName);

        if (tool is null)
        {
            return false;
        }

        return tool.RequiredPermissions.All(permissions.Contains);
    }

    private static void MarkUnselected(List<AiProposedChange> changes, List<AiProposedChange> selected)
    {
        var selectedIds = selected.Select(change => change.Id).ToHashSet();

        foreach (var change in changes)
        {
            var wasSelected = selectedIds.Contains(change.Id);

            if (wasSelected)
            {
                continue;
            }

            change.ApplyStatus = AiChangeApplyStatus.Skipped;
        }
    }

    private static AiChangeSetStatus ResolveStatus(List<AiAppliedChangeResult> results)
    {
        var applied = results.Count(result => result.Status == AiChangeApplyStatus.Applied);
        var everythingApplied = applied == results.Count;

        if (everythingApplied)
        {
            return AiChangeSetStatus.Applied;
        }

        return applied == 0 ? AiChangeSetStatus.Pending : AiChangeSetStatus.PartiallyApplied;
    }

    private async Task<AiAppliedChangeResult> ApplyChange(
        AiProposedChange change,
        Dictionary<string, int> resolvedRefs,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = ResolveHandler(change.ToolName);

            if (handler is null)
            {
                var unsupported = AiChangePayload.Failure(change, $"No handler is registered for {change.ToolName}.");

                change.ApplyStatus = unsupported.Status;
                change.ApplyError = unsupported.Error;

                return unsupported;
            }

            var applyContext = new AiChangeApplyContext { Change = change, ResolvedRefs = resolvedRefs };
            var outcome = await handler.Apply(applyContext, cancellationToken);

            change.ApplyStatus = outcome.Status;
            change.ApplyError = outcome.Error;
            change.AppliedEntityId = outcome.AppliedEntityId;

            var hasRef = change.RefKey is not null && outcome.AppliedEntityId.HasValue;

            if (hasRef)
            {
                resolvedRefs[change.RefKey!] = outcome.AppliedEntityId!.Value;
            }

            return outcome;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            change.ApplyStatus = AiChangeApplyStatus.Failed;
            change.ApplyError = exception.Message;

            return new AiAppliedChangeResult
            {
                ChangeId = change.Id,
                Status = AiChangeApplyStatus.Failed,
                Error = exception.Message,
            };
        }
    }

    private IAiChangeHandler? ResolveHandler(string toolName)
    {
        var found = HandlersByToolName.TryGetValue(toolName, out var handler);

        return found ? handler : null;
    }
}
