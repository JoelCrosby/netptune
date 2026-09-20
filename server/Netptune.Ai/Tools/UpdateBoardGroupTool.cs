using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Handlers.BoardGroups.Queries;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateBoardGroupTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateBoardGroupTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_update_board_group";

    public string Description =>
        "Propose renaming a board group, the column on a board, or changing the status tasks take when they are "
        + "moved into it. Pass clearStatus to leave the group without a status of its own.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.BoardGroups.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "boardGroupId": { "type": "integer", "description": "The id of the group to change, from list_board_groups." },
          "name": { "type": "string", "description": "New group name." },
          "statusId": { "type": "integer", "description": "Status tasks take when moved into this group." },
          "clearStatus": { "type": "boolean", "description": "Remove the status the group applies, instead of setting one." }
        }
        """,
        "boardGroupId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var boardGroupId = AiToolSchema.GetInt(arguments, "boardGroupId");

        if (!boardGroupId.HasValue)
        {
            return AiToolExecution.Failed("A boardGroupId is required.");
        }

        var option = await AiBoardLookup.FindGroup(Mediator, boardGroupId.Value, cancellationToken);

        if (option is null)
        {
            return AiToolExecution.Failed($"Board group {boardGroupId} is not in this workspace.");
        }

        var group = await Mediator.Send(new GetBoardGroupQuery(boardGroupId.Value), cancellationToken);

        if (group is null)
        {
            return AiToolExecution.Failed($"Board group {boardGroupId} is not in this workspace.");
        }

        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var fields = new List<AiChangeField>();
        var payload = new Dictionary<string, object> { ["boardGroupId"] = group.Id };
        var isRenamed = !string.IsNullOrWhiteSpace(name) && !string.Equals(name, group.Name, StringComparison.Ordinal);

        if (isRenamed)
        {
            fields.Add(AiChangeFields.Text("name", group.Name, name));
            payload["name"] = name!;
        }

        var statusChange = await ResolveStatusChange(arguments, group.StatusId, cancellationToken);

        if (statusChange.Error is not null)
        {
            return AiToolExecution.Failed(statusChange.Error);
        }

        if (statusChange.Field is not null)
        {
            fields.Add(statusChange.Field);

            if (statusChange.IsCleared)
            {
                payload["clearStatus"] = true;
            }
            else
            {
                payload["statusId"] = statusChange.StatusId!.Value;
            }
        }

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this board group.");
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "boardGroup",
            EntityId = group.Id,
            Summary = $"Update {changedNames} on “{group.Name}” in {option.BoardName}",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed updating board group {group.Id}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private sealed record AiStatusChange
    {
        public AiChangeField? Field { get; init; }

        public int? StatusId { get; init; }

        public bool IsCleared { get; init; }

        public string? Error { get; init; }

        public static AiStatusChange None { get; } = new();

        public static AiStatusChange Failed(string error)
        {
            return new AiStatusChange { Error = error };
        }
    }

    private async Task<AiStatusChange> ResolveStatusChange(
        JsonElement arguments,
        int? currentStatusId,
        CancellationToken cancellationToken)
    {
        var clearStatus = AiToolSchema.GetBool(arguments, "clearStatus") ?? false;
        var statusId = AiToolSchema.GetInt(arguments, "statusId");
        var isUnchanged = !clearStatus && (!statusId.HasValue || statusId == currentStatusId);

        if (isUnchanged)
        {
            return AiStatusChange.None;
        }

        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
        var current = statuses?.FirstOrDefault(item => item.Id == currentStatusId);
        var before = ToValues(current);

        if (clearStatus)
        {
            var hasStatus = currentStatusId.HasValue;

            if (!hasStatus)
            {
                return AiStatusChange.None;
            }

            return new AiStatusChange
            {
                Field = AiChangeFields.Values("status", AiChangeValueKind.Status, before, []),
                IsCleared = true,
            };
        }

        var status = statuses?.FirstOrDefault(item => item.Id == statusId!.Value);

        if (status is null)
        {
            return AiStatusChange.Failed($"Status {statusId} is not a task status in this workspace.");
        }

        return new AiStatusChange
        {
            Field = AiChangeFields.Values("status", AiChangeValueKind.Status, before, ToValues(status)),
            StatusId = status.Id,
        };
    }

    private static List<AiChangeValue> ToValues(StatusViewModel? status)
    {
        if (status is null)
        {
            return [];
        }

        return [AiChangeFields.Status(status.Id, status.Name, status.Color)];
    }
}
