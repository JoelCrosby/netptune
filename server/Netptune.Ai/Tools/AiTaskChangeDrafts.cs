using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Tools;

internal sealed record AiTaskTarget
{
    public TaskViewModel? Task { get; init; }

    public string? RefKey { get; init; }

    public required string Name { get; init; }
}

internal sealed record AiTaskDraftResult
{
    public AiChangeDraft? Draft { get; init; }

    public string? Error { get; init; }

    public static readonly AiTaskDraftResult Unchanged = new();

    public static AiTaskDraftResult Proposed(AiChangeDraft draft)
    {
        return new AiTaskDraftResult { Draft = draft };
    }

    public static AiTaskDraftResult Failed(string error)
    {
        return new AiTaskDraftResult { Error = error };
    }
}

internal static class AiTaskChangeDrafts
{
    public const string AssignChange = "propose_assign_task";
    public const string TagsChange = "propose_set_task_tags";
    public const string BoardGroupChange = "propose_move_task_to_board_group";
    public const string BoardRemovalChange = "propose_remove_task_from_board";

    public static async Task<AiTaskDraftResult> Assignees(
        IMediator mediator,
        TaskViewModel task,
        JsonElement arguments,
        AiTaskLookups? lookups,
        CancellationToken cancellationToken)
    {
        var assigneeIds = AiToolSchema.GetStringArray(arguments, "assigneeIds")
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var currentIds = task.Assignees.Select(assignee => assignee.Id).ToHashSet(StringComparer.Ordinal);
        var isUnchanged = currentIds.SetEquals(assigneeIds);

        if (isUnchanged)
        {
            return AiTaskDraftResult.Unchanged;
        }

        var members = lookups?.Members
            ?? (await mediator.Send(new GetAssigneesByIdQuery(assigneeIds), cancellationToken))
                .ToDictionary(member => member.Id, StringComparer.Ordinal);
        var unknownIds = assigneeIds.Where(id => !members.ContainsKey(id)).ToList();

        if (unknownIds.Count > 0)
        {
            return AiTaskDraftResult.Failed(
                $"These assignee ids are not in this workspace: {string.Join(", ", unknownIds)}.");
        }

        var before = task.Assignees
            .Select(assignee => AiChangeFields.User(assignee.Id, assignee.DisplayName, assignee.PictureUrl))
            .ToList();

        var after = assigneeIds
            .Select(id => AiChangeFields.User(id, members[id].DisplayName, members[id].PictureUrl))
            .ToList();

        var payload = new JsonObject
        {
            ["taskId"] = task.Id,
            ["assigneeIds"] = JsonSerializer.SerializeToNode(assigneeIds),
        };

        return AiTaskDraftResult.Proposed(new AiChangeDraft
        {
            ToolName = AssignChange,
            EntityType = "task",
            EntityId = task.Id,
            Summary = Localized.Format(AppText.AiSummarySetAssignees, task.Name),
            Fields = [AiChangeFields.Values("assignees", AiChangeValueKind.User, before, after)],
            Payload = JsonDocument.Parse(payload.ToJsonString()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });
    }

    public static async Task<AiTaskDraftResult> Tags(
        IMediator mediator,
        IAiChangeSetBuilder changeSet,
        AiTaskTarget target,
        JsonElement arguments,
        AiTaskLookups? lookups,
        CancellationToken cancellationToken)
    {
        var requested = AiTagVocabulary.ReadRequested(arguments);
        var currentTags = target.Task?.Tags ?? [];
        var isExistingTask = target.Task is not null;
        var isUnchanged = currentTags.ToHashSet(StringComparer.OrdinalIgnoreCase).SetEquals(requested);
        var isUnchangedOnExistingTask = isExistingTask && isUnchanged;

        if (isUnchangedOnExistingTask)
        {
            return AiTaskDraftResult.Unchanged;
        }

        var unknownError = await AiTagVocabulary.FindUnknown(
            mediator,
            changeSet,
            requested,
            lookups?.ExistingTagNames,
            cancellationToken);

        if (unknownError is not null)
        {
            return AiTaskDraftResult.Failed(unknownError);
        }

        var before = currentTags.Select(AiChangeFields.Tag).ToList();
        var after = requested.Select(AiChangeFields.Tag).ToList();
        var payload = new JsonObject { ["tags"] = JsonSerializer.SerializeToNode(requested) };

        if (target.Task is not null)
        {
            payload["taskId"] = target.Task.Id;
        }

        if (target.RefKey is not null)
        {
            payload["taskRef"] = target.RefKey;
        }

        return AiTaskDraftResult.Proposed(new AiChangeDraft
        {
            ToolName = TagsChange,
            EntityType = "task",
            EntityId = target.Task?.Id,
            Summary = Localized.Format(AppText.AiSummarySetTags, target.Name),
            Fields = [AiChangeFields.Values("tags", AiChangeValueKind.Tag, before, after)],
            Payload = JsonDocument.Parse(payload.ToJsonString()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });
    }

    public static async Task<AiTaskDraftResult> BoardGroup(
        IMediator mediator,
        TaskViewModel task,
        int boardGroupId,
        int? requestedStatusId,
        AiTaskLookups? lookups,
        CancellationToken cancellationToken)
    {
        var isUnchanged = task.BoardGroupId == boardGroupId
            || task.Placements.Any(placement => placement.BoardGroupId == boardGroupId);

        if (isUnchanged)
        {
            return AiTaskDraftResult.Unchanged;
        }

        var group = await AiBoardLookup.FindGroup(mediator, boardGroupId, lookups, cancellationToken);

        if (group is null)
        {
            return AiTaskDraftResult.Failed($"Board group {boardGroupId} is not in this workspace.");
        }

        var mismatch = AiBoardLookup.FindProjectMismatch(group, task.ProjectId);

        if (mismatch is not null)
        {
            return AiTaskDraftResult.Failed(mismatch);
        }

        var groupField = AiChangeFields.BoardGroup(group.Id, $"{group.BoardName} · {group.Name}");
        var fields = new List<AiChangeField> { groupField };
        var groupStatus = await AiBoardLookup.FindGroupStatus(mediator, group.Id, lookups, cancellationToken);
        var changesStatus = groupStatus is not null && groupStatus.Id != task.StatusId;

        var conflictsWithRequest = changesStatus && requestedStatusId.HasValue && requestedStatusId != groupStatus!.Id;

        if (conflictsWithRequest)
        {
            return AiTaskDraftResult.Failed(AiBoardLookup.StatusConflict(group, groupStatus!));
        }

        if (changesStatus)
        {
            fields.Add(AiChangeFields.Values(
                "status",
                AiChangeValueKind.Status,
                [AiChangeFields.Status(task.StatusId, task.StatusName, task.StatusColor)],
                [AiChangeFields.Status(groupStatus!.Id, groupStatus.Name, groupStatus.Color)]));
        }

        var payload = new
        {
            taskId = task.Id,
            projectId = task.ProjectId,
            boardGroupId = group.Id,
            boardId = group.BoardId,
            boardIdentifier = group.BoardIdentifier,
        };

        return AiTaskDraftResult.Proposed(new AiChangeDraft
        {
            ToolName = BoardGroupChange,
            EntityType = "task",
            EntityId = task.Id,
            Summary = Localized.Format(AppText.AiSummaryMoveTaskToBoardGroup, task.Name, group.Name, group.BoardName),
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });
    }

    public static AiChangeDraft BoardRemoval(TaskViewModel task, BoardPlacementViewModel placement)
    {
        var payload = new
        {
            taskId = task.Id,
            boardId = placement.BoardId,
        };

        var current = new AiChangeValue
        {
            Id = placement.BoardGroupId.ToString(CultureInfo.InvariantCulture),
            Display = $"{placement.BoardName} · {placement.BoardGroupName}",
        };

        return new AiChangeDraft
        {
            ToolName = BoardRemovalChange,
            EntityType = "task",
            EntityId = task.Id,
            Summary = Localized.Format(AppText.AiSummaryRemoveTaskFromBoard, task.Name, placement.BoardName),
            Fields = [AiChangeFields.Values("boardGroup", AiChangeValueKind.Text, [current], [])],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        };
    }

    // A task can only sit on boards in its own project, and an existing task can never be in a project
    // this change set has not created yet.
    public static AiTaskDraftResult PendingBoardGroup(TaskViewModel task, AiPendingBoardGroup group)
    {
        var mismatch = AiPendingBoardGroupLookup.FindProjectMismatch(group, task.ProjectId, null);

        if (mismatch is not null)
        {
            return AiTaskDraftResult.Failed(mismatch);
        }

        var payload = new
        {
            taskId = task.Id,
            projectId = task.ProjectId,
            boardGroupRef = group.RefKey,
        };

        return AiTaskDraftResult.Proposed(new AiChangeDraft
        {
            ToolName = BoardGroupChange,
            EntityType = "task",
            EntityId = task.Id,
            Summary = Localized.Format(AppText.AiSummaryMoveTaskToBoardGroup, task.Name, group.Name, group.BoardName),
            Fields = [AiChangeFields.BoardGroup(null, group.Label)],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });
    }
}
