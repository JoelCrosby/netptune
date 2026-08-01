using System.Text.Json;

using Mediator;

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

    public AiChangeSetApplier(
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IMediator mediator,
        IAiToolRegistry tools)
    {
        UnitOfWork = unitOfWork;
        Identity = identity;
        Mediator = mediator;
        Tools = tools;
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

        foreach (var change in selected)
        {
            var result = await ApplyChange(change, resolvedRefs, cancellationToken);

            results.Add(result);
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
            var isCreate = string.Equals(change.ToolName, "propose_create_task", StringComparison.Ordinal);
            var outcome = isCreate
                ? await ApplyCreateTask(change, cancellationToken)
                : await ApplyUpdateTask(change, resolvedRefs, cancellationToken);

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

    private async Task<AiAppliedChangeResult> ApplyCreateTask(
        AiProposedChange change,
        CancellationToken cancellationToken)
    {
        var payload = change.Payload.RootElement;
        var request = new AddProjectTaskRequest
        {
            Name = ReadString(payload, "name") ?? string.Empty,
            Description = ReadString(payload, "description") ?? string.Empty,
            ProjectId = ReadInt(payload, "projectId"),
            DueDate = ReadDate(payload, "dueDate"),
        };

        var response = await Mediator.Send(new CreateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return Failure(change, response.Message ?? "The task could not be created.");
        }

        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Applied,
            AppliedEntityId = response.Payload?.Id,
        };
    }

    private async Task<AiAppliedChangeResult> ApplyUpdateTask(
        AiProposedChange change,
        Dictionary<string, int> resolvedRefs,
        CancellationToken cancellationToken)
    {
        var payload = change.Payload.RootElement;
        var taskId = ResolveTaskId(change, payload, resolvedRefs);

        if (!taskId.HasValue)
        {
            return Failure(change, "The task this change refers to could not be resolved.");
        }

        var request = new UpdateProjectTaskRequest
        {
            Id = taskId.Value,
            Name = ReadString(payload, "name"),
            Description = ReadString(payload, "description"),
            StatusId = ReadInt(payload, "statusId"),
        };

        var dueDate = ReadDate(payload, "dueDate");

        if (dueDate.HasValue)
        {
            request.DueDate = dueDate;
        }

        var response = await Mediator.Send(new UpdateTaskCommand(request), cancellationToken);

        if (!response.IsSuccess)
        {
            return Failure(change, response.Message ?? "The task could not be updated.");
        }

        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Applied,
            AppliedEntityId = taskId,
        };
    }

    private static int? ResolveTaskId(
        AiProposedChange change,
        JsonElement payload,
        Dictionary<string, int> resolvedRefs)
    {
        if (change.EntityId.HasValue)
        {
            return change.EntityId;
        }

        var refKey = ReadString(payload, "taskRef");
        var hasRef = refKey is not null && resolvedRefs.TryGetValue(refKey, out var resolved);

        if (hasRef)
        {
            return resolvedRefs[refKey!];
        }

        return ReadInt(payload, "taskId");
    }

    private static AiAppliedChangeResult Failure(AiProposedChange change, string message)
    {
        return new AiAppliedChangeResult
        {
            ChangeId = change.Id,
            Status = AiChangeApplyStatus.Failed,
            Error = message,
        };
    }

    private static string? ReadString(JsonElement payload, string name)
    {
        var hasProperty = payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String;

        return hasProperty ? payload.GetProperty(name).GetString() : null;
    }

    private static int? ReadInt(JsonElement payload, string name)
    {
        var isObject = payload.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return null;
        }

        var hasProperty = payload.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number;

        return hasProperty ? value.GetInt32() : null;
    }

    private static DateOnly? ReadDate(JsonElement payload, string name)
    {
        var raw = ReadString(payload, name);
        var isParsed = DateOnly.TryParse(raw, out var parsed);

        return isParsed ? parsed : null;
    }
}
