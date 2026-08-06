using Microsoft.Extensions.Logging;

using Netptune.Ai.Execution.Handlers;
using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Ai.Execution;

public sealed class AiChangeSetApplier : IAiChangeSetApplier
{
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiToolRegistry Tools;
    private readonly IAiExecutionContext AiExecution;
    private readonly ILogger<AiChangeSetApplier> Logger;
    private readonly Dictionary<string, IAiChangeHandler> HandlersByToolName;

    public AiChangeSetApplier(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiToolRegistry tools,
        IAiExecutionContext aiExecution,
        ILogger<AiChangeSetApplier> logger,
        IEnumerable<IAiChangeHandler> handlers)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Tools = tools;
        AiExecution = aiExecution;
        Logger = logger;
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
        var ordered = OrderByDependency(selected);

        using (AiExecution.Begin(agent, changeSet.CorrelationId))
        {
            foreach (var change in ordered)
            {
                var blocker = FindUnmetReference(change, resolvedRefs);
                var result = blocker is null
                    ? await ApplyChange(change, resolvedRefs, cancellationToken)
                    : SkipChange(change, blocker);

                results.Add(result);
            }
        }

        MarkUnselected(changes, selected);

        changeSet.Status = ResolveStatus(results);
        changeSet.AppliedAt = DateTime.UtcNow;

        try
        {
            await RecordOutcome(changeSet, ordered, results, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "The applied change set could not be recorded in the conversation");
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return new AiApplyResult
        {
            ChangeSetId = changeSet.Id,
            Status = changeSet.Status,
            Results = results,
        };
    }

    private async Task RecordOutcome(
        AiChangeSet changeSet,
        List<AiProposedChange> applied,
        List<AiAppliedChangeResult> results,
        CancellationToken cancellationToken)
    {
        var summary = await DescribeOutcome(applied, results, cancellationToken);
        var conversation = await UnitOfWork.AiConversations.GetAsync(
            changeSet.ConversationId,
            true,
            cancellationToken);

        if (conversation is null)
        {
            return;
        }

        var content = AiMessageContent.FromChatMessage(new AiChatMessage
        {
            Role = AiMessageRole.User,
            Text = summary,
        });

        var sequence = await UnitOfWork.AiConversations.GetNextSequence(conversation.Id, cancellationToken);
        var record = new AiMessage
        {
            ConversationId = conversation.Id,
            Sequence = sequence,
            Role = AiMessageRole.User,
            Content = content.ToJsonDocument(),
            Provider = conversation.Provider,
            Model = conversation.Model,
            Status = AiMessageStatus.Complete,
            CreatedAt = DateTime.UtcNow,
        };

        await UnitOfWork.AiConversations.AddMessage(record, cancellationToken);

        conversation.MessageCount += 1;
        conversation.LastMessageAt = record.CreatedAt;
    }

    private async Task<string> DescribeOutcome(
        List<AiProposedChange> applied,
        List<AiAppliedChangeResult> results,
        CancellationToken cancellationToken)
    {
        var byChangeId = applied.ToDictionary(change => change.Id);
        var appliedResults = results.Where(result => result.Status == AiChangeApplyStatus.Applied).ToList();
        var systemIds = await ReadTaskSystemIds(appliedResults, byChangeId, cancellationToken);
        var lines = new List<string>();

        foreach (var result in results)
        {
            var change = byChangeId[result.ChangeId];
            var identifier = DescribeIdentifier(result, change, systemIds);

            lines.Add($"- {change.Summary}: {result.Status.ToString().ToLowerInvariant()}{identifier}");
        }

        var outcome = string.Join("\n", lines);

        return $"I applied the change set. Use these ids for any follow-up work rather than searching for them.\n{outcome}";
    }

    private static string DescribeIdentifier(
        AiAppliedChangeResult result,
        AiProposedChange change,
        IReadOnlyDictionary<int, string> systemIds)
    {
        var isApplied = result.Status == AiChangeApplyStatus.Applied;

        if (!isApplied)
        {
            return result.Error is null ? string.Empty : $" ({result.Error})";
        }

        var entityId = result.AppliedEntityId ?? change.EntityId;

        if (!entityId.HasValue)
        {
            return string.Empty;
        }

        var hasSystemId = systemIds.TryGetValue(entityId.Value, out var systemId);

        return hasSystemId ? $" ({systemId}, id {entityId})" : $" ({change.EntityType} id {entityId})";
    }

    private async Task<Dictionary<int, string>> ReadTaskSystemIds(
        List<AiAppliedChangeResult> results,
        IReadOnlyDictionary<long, AiProposedChange> byChangeId,
        CancellationToken cancellationToken)
    {
        var taskIds = results
            .Where(result => string.Equals(byChangeId[result.ChangeId].EntityType, "task", StringComparison.Ordinal))
            .Select(result => result.AppliedEntityId ?? byChangeId[result.ChangeId].EntityId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (taskIds.Count == 0)
        {
            return [];
        }

        var models = await UnitOfWork.Tasks.GetTaskViewModels(taskIds, cancellationToken);

        return models.ToDictionary(model => model.Id, model => model.SystemId);
    }

    private static List<AiProposedChange> OrderByDependency(List<AiProposedChange> changes)
    {
        var byRefKey = changes
            .Where(change => change.RefKey is not null)
            .GroupBy(change => change.RefKey!, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

        var ordered = new List<AiProposedChange>();
        var placed = new HashSet<long>();
        var placing = new HashSet<long>();

        foreach (var change in changes)
        {
            Place(change, byRefKey, ordered, placed, placing);
        }

        return ordered;
    }

    private static void Place(
        AiProposedChange change,
        IReadOnlyDictionary<string, AiProposedChange> byRefKey,
        List<AiProposedChange> ordered,
        HashSet<long> placed,
        HashSet<long> placing)
    {
        var isSettled = placed.Contains(change.Id) || !placing.Add(change.Id);

        if (isSettled)
        {
            return;
        }

        foreach (var reference in AiChangePayload.ReadReferences(change.Payload.RootElement))
        {
            var isKnown = byRefKey.TryGetValue(reference, out var prerequisite);

            if (isKnown)
            {
                Place(prerequisite!, byRefKey, ordered, placed, placing);
            }
        }

        placing.Remove(change.Id);
        placed.Add(change.Id);
        ordered.Add(change);
    }

    private static string? FindUnmetReference(
        AiProposedChange change,
        IReadOnlyDictionary<string, int> resolvedRefs)
    {
        return AiChangePayload
            .ReadReferences(change.Payload.RootElement)
            .FirstOrDefault(reference => !resolvedRefs.ContainsKey(reference));
    }

    private static AiAppliedChangeResult SkipChange(AiProposedChange change, string reference)
    {
        var error = $"Skipped because {reference} was not created.";

        change.ApplyStatus = AiChangeApplyStatus.Skipped;
        change.ApplyError = error;

        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Skipped,
            Error = error,
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

        var required = tool.GetRequiredPermissions(change.Payload.RootElement);

        return required.All(permissions.Contains);
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

            await CaptureUndo(handler, applyContext, cancellationToken);

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

    /// <summary>
    /// The prior state only exists until the change lands, so it is read first
    /// and stored with the change. A handler that cannot be undone stores nothing.
    /// </summary>
    private async Task CaptureUndo(
        IAiChangeHandler handler,
        AiChangeApplyContext context,
        CancellationToken cancellationToken)
    {
        var isUndoable = handler is IAiChangeUndoHandler;

        if (!isUndoable)
        {
            return;
        }

        try
        {
            context.Change.UndoPayload = await ((IAiChangeUndoHandler)handler).Capture(context, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "The undo snapshot for {Tool} could not be captured", handler.ToolName);
        }
    }

    public async Task<AiApplyResult?> Undo(Guid changeSetId, CancellationToken cancellationToken)
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

        var wasApplied = changeSet.AppliedAt.HasValue;

        if (!wasApplied)
        {
            throw new InvalidOperationException("Only an applied change set can be undone.");
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);
        var undoable = changes
            .Where(change => change.ApplyStatus == AiChangeApplyStatus.Applied)
            .Where(change => !change.UndoneAt.HasValue)
            .ToList();

        if (undoable.Count == 0)
        {
            throw new InvalidOperationException("There is nothing left to undo in this change set.");
        }

        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            userId,
            workspaceKey,
            cancellationToken: cancellationToken);

        if (membership is null)
        {
            throw new InvalidOperationException("You are not a member of this workspace.");
        }

        var permissions = membership.Permissions.ToHashSet(StringComparer.Ordinal);
        var missingPermission = undoable.Any(change => !CanUndo(change, permissions));

        if (missingPermission)
        {
            throw new UnauthorizedAccessException("You do not have permission to undo these changes.");
        }

        var agent = await ResolveAgentName(changeSet, cancellationToken);
        var results = new List<AiAppliedChangeResult>();

        using (AiExecution.Begin(agent, changeSet.CorrelationId))
        {
            /* A change set is applied in dependency order, so it comes apart in reverse. */
            foreach (var change in OrderByDependency(undoable).AsEnumerable().Reverse())
            {
                var result = await UndoChange(change, cancellationToken);

                results.Add(result);
            }
        }

        var undoneEverything = changes
            .Where(change => change.ApplyStatus == AiChangeApplyStatus.Applied)
            .All(change => change.UndoneAt.HasValue);

        if (undoneEverything)
        {
            changeSet.UndoneAt = DateTime.UtcNow;
        }

        try
        {
            await RecordUndo(changeSet, results, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "The undone change set could not be recorded in the conversation");
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return new AiApplyResult
        {
            ChangeSetId = changeSet.Id,
            Status = changeSet.Status,
            Results = results,
        };
    }

    private async Task<AiAppliedChangeResult> UndoChange(
        AiProposedChange change,
        CancellationToken cancellationToken)
    {
        try
        {
            var handler = ResolveHandler(change.ToolName) as IAiChangeUndoHandler;

            if (handler is null)
            {
                return AiChangeUndoResult.Failure(change, $"A {change.ToolName} change cannot be undone.");
            }

            var outcome = await handler.Revert(new AiChangeUndoContext { Change = change }, cancellationToken);
            var isUndone = outcome.Status == AiChangeApplyStatus.Applied;

            if (isUndone)
            {
                change.UndoneAt = DateTime.UtcNow;
            }

            return outcome;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AiChangeUndoResult.Failure(change, exception.Message);
        }
    }

    private bool CanUndo(AiProposedChange change, IReadOnlySet<string> permissions)
    {
        var handler = ResolveHandler(change.ToolName) as IAiChangeUndoHandler;

        if (handler is null)
        {
            return true;
        }

        return handler.UndoPermissions.All(permissions.Contains);
    }

    private async Task RecordUndo(
        AiChangeSet changeSet,
        List<AiAppliedChangeResult> results,
        CancellationToken cancellationToken)
    {
        var undone = results.Count(result => result.Status == AiChangeApplyStatus.Applied);
        var failed = results.Count - undone;
        var tail = failed == 0 ? string.Empty : $" {failed} could not be undone and are still in place.";
        var summary = $"I undid the change set. {undone} of {results.Count} changes were reverted.{tail}";

        var conversation = await UnitOfWork.AiConversations.GetAsync(
            changeSet.ConversationId,
            true,
            cancellationToken);

        if (conversation is null)
        {
            return;
        }

        var content = AiMessageContent.FromChatMessage(new AiChatMessage
        {
            Role = AiMessageRole.User,
            Text = summary,
        });

        var sequence = await UnitOfWork.AiConversations.GetNextSequence(conversation.Id, cancellationToken);
        var record = new AiMessage
        {
            ConversationId = conversation.Id,
            Sequence = sequence,
            Role = AiMessageRole.User,
            Content = content.ToJsonDocument(),
            Provider = conversation.Provider,
            Model = conversation.Model,
            Status = AiMessageStatus.Complete,
            CreatedAt = DateTime.UtcNow,
        };

        await UnitOfWork.AiConversations.AddMessage(record, cancellationToken);

        conversation.MessageCount += 1;
        conversation.LastMessageAt = record.CreatedAt;
    }

    public async Task<AiApplyResult?> RetryFailed(Guid changeSetId, CancellationToken cancellationToken)
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

        var wasApplied = changeSet.AppliedAt.HasValue;

        if (!wasApplied)
        {
            throw new InvalidOperationException("Only an applied change set has failed changes to retry.");
        }

        var isUndone = changeSet.UndoneAt.HasValue;

        if (isUndone)
        {
            throw new InvalidOperationException("An undone change set cannot be applied again.");
        }

        var changes = await UnitOfWork.AiChangeSets.GetChanges(changeSet.Id, cancellationToken);
        var failed = changes.Where(change => change.ApplyStatus == AiChangeApplyStatus.Failed).ToList();

        if (failed.Count == 0)
        {
            throw new InvalidOperationException("There is nothing left to retry in this change set.");
        }

        var membership = await UnitOfWork.WorkspaceUsers.GetUserPermissions(
            userId,
            workspaceKey,
            cancellationToken: cancellationToken);

        if (membership is null)
        {
            throw new InvalidOperationException("You are not a member of this workspace.");
        }

        var permissions = membership.Permissions.ToHashSet(StringComparer.Ordinal);
        var missingPermission = failed.Any(change => !HasPermission(change, permissions));

        if (missingPermission)
        {
            throw new UnauthorizedAccessException("You do not have permission to apply these changes.");
        }

        var resolvedRefs = ReadResolvedRefs(changes);
        var agent = await ResolveAgentName(changeSet, cancellationToken);
        var ordered = OrderByDependency(failed);
        var results = new List<AiAppliedChangeResult>();

        using (AiExecution.Begin(agent, changeSet.CorrelationId))
        {
            foreach (var change in ordered)
            {
                var blocker = FindUnmetReference(change, resolvedRefs);
                var result = blocker is null
                    ? await ApplyChange(change, resolvedRefs, cancellationToken)
                    : SkipChange(change, blocker);

                results.Add(result);
            }
        }

        changeSet.Status = ResolveRetriedStatus(changes);

        try
        {
            await RecordOutcome(changeSet, ordered, results, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Logger.LogWarning(exception, "The retried change set could not be recorded in the conversation");
        }

        await UnitOfWork.CompleteAsync(cancellationToken);

        return new AiApplyResult
        {
            ChangeSetId = changeSet.Id,
            Status = changeSet.Status,
            Results = results,
        };
    }

    private static Dictionary<string, int> ReadResolvedRefs(List<AiProposedChange> changes)
    {
        var resolved = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var change in changes)
        {
            var isResolved = change.RefKey is not null
                && change.ApplyStatus == AiChangeApplyStatus.Applied
                && change.AppliedEntityId.HasValue;

            if (isResolved)
            {
                resolved[change.RefKey!] = change.AppliedEntityId!.Value;
            }
        }

        return resolved;
    }

    private static AiChangeSetStatus ResolveRetriedStatus(List<AiProposedChange> changes)
    {
        var touched = changes
            .Where(change => change.ApplyStatus != AiChangeApplyStatus.Skipped)
            .ToList();

        var applied = touched.Count(change => change.ApplyStatus == AiChangeApplyStatus.Applied);
        var everythingApplied = touched.Count > 0 && applied == touched.Count;

        if (everythingApplied)
        {
            return AiChangeSetStatus.Applied;
        }

        return applied == 0 ? AiChangeSetStatus.Pending : AiChangeSetStatus.PartiallyApplied;
    }
}
