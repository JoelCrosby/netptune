using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Users;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Tools;

internal sealed record AiTaskTarget
{
    public TaskViewModel? Task { get; init; }

    public string? RefKey { get; init; }

    public required string Name { get; init; }
}

internal sealed record AiTaskDraftResult(AiChangeDraft? Draft, string? Error)
{
    public static readonly AiTaskDraftResult Unchanged = new(null, null);

    public static AiTaskDraftResult Proposed(AiChangeDraft draft)
    {
        return new AiTaskDraftResult(draft, null);
    }

    public static AiTaskDraftResult Failed(string error)
    {
        return new AiTaskDraftResult(null, error);
    }
}

internal static class AiTaskChangeDrafts
{
    public const string AssignChange = "propose_assign_task";
    public const string TagsChange = "propose_set_task_tags";
    public const string BoardGroupChange = "propose_move_task_to_board_group";

    private const int MemberLookupPageSize = 200;

    public static async Task<AiTaskDraftResult> Assignees(
        IMediator mediator,
        TaskViewModel task,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var assigneeIds = AiToolSchema.GetStringArray(arguments, "assigneeIds").Distinct(StringComparer.Ordinal).ToList();
        var currentIds = task.Assignees.Select(assignee => assignee.Id).ToHashSet(StringComparer.Ordinal);
        var isUnchanged = currentIds.SetEquals(assigneeIds);

        if (isUnchanged)
        {
            return AiTaskDraftResult.Unchanged;
        }

        var members = await LoadMembers(mediator, cancellationToken);

        if (members is null)
        {
            return AiTaskDraftResult.Failed("Workspace members could not be read.");
        }

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
            Summary = $"Set assignees on “{task.Name}”",
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

        var knownNames = await AiTagVocabulary.Read(mediator, changeSet, cancellationToken);

        if (knownNames is null)
        {
            return AiTaskDraftResult.Failed("Workspace tags could not be read.");
        }

        var unknownError = AiTagVocabulary.FindUnknown(requested, knownNames);

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
            Summary = $"Set tags on “{target.Name}”",
            Fields = [AiChangeFields.Values("tags", AiChangeValueKind.Tag, before, after)],
            Payload = JsonDocument.Parse(payload.ToJsonString()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });
    }

    public static async Task<AiTaskDraftResult> BoardGroup(
        IMediator mediator,
        TaskViewModel task,
        int boardGroupId,
        CancellationToken cancellationToken)
    {
        var isUnchanged = task.BoardGroupId == boardGroupId;

        if (isUnchanged)
        {
            return AiTaskDraftResult.Unchanged;
        }

        var options = await mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);
        var group = options.FirstOrDefault(option => option.Id == boardGroupId);

        if (group is null)
        {
            return AiTaskDraftResult.Failed($"Board group {boardGroupId} is not in this workspace.");
        }

        var payload = new
        {
            taskId = task.Id,
            boardGroupId = group.Id,
            boardIdentifier = group.BoardIdentifier,
        };

        return AiTaskDraftResult.Proposed(new AiChangeDraft
        {
            ToolName = BoardGroupChange,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Move “{task.Name}” to {group.Name} on {group.BoardName}",
            Fields = [new AiChangeField { Name = "boardGroup", After = $"{group.BoardName} · {group.Name}" }],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });
    }

    private static async Task<Dictionary<string, AssigneeViewModel>?> LoadMembers(
        IMediator mediator,
        CancellationToken cancellationToken)
    {
        var filter = new AssigneeFilter { Page = 1, PageSize = MemberLookupPageSize };
        var result = await mediator.Send(new GetAssigneesQuery(filter), cancellationToken);

        if (!result.IsSuccess)
        {
            return null;
        }

        var members = result.Payload?.Items ?? [];

        return members.ToDictionary(member => member.Id, member => member, StringComparer.Ordinal);
    }
}
