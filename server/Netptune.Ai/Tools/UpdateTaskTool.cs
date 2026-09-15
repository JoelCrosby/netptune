using System.Text.Json;
using System.Text.Json.Nodes;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateTaskTool : IAiTool
{
    public const string UpdateChange = "propose_update_task";

    private const decimal SmallestTShirtSize = 1;
    private const decimal LargestTShirtSize = 5;

    private static readonly string[] ClearableFields = ["startDate", "dueDate"];

    private static readonly string[] FieldArguments =
    [
        "name",
        "description",
        "statusId",
        "priority",
        "startDate",
        "dueDate",
        "estimateType",
        "estimateValue",
        "clear",
    ];

    private static readonly Dictionary<string, string> PermissionsByChange = new(StringComparer.Ordinal)
    {
        [UpdateChange] = NetptunePermissions.Tasks.Update,
        [AiTaskChangeDrafts.AssignChange] = NetptunePermissions.Tasks.Reassign,
        [AiTaskChangeDrafts.TagsChange] = NetptunePermissions.Tags.Assign,
        [AiTaskChangeDrafts.BoardGroupChange] = NetptunePermissions.Tasks.Move,
    };

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateTaskTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => UpdateChange;

    public string Description =>
        "Propose changing an existing task: name, description, status, priority, dates, estimate, "
        + "assignees, tags or board group. Send only what should change. "
        + "A task proposed earlier in this change set can take tags here by taskRef; set the rest when proposing it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public IReadOnlyList<string> ProposedChanges { get; } = [.. PermissionsByChange.Keys];

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the task to change." },
          "taskRef": { "type": "string", "description": "Handle of a task proposed earlier in this change set, to tag it." },
          "name": { "type": "string", "description": "New task name." },
          "description": { "type": "string", "description": "New task description." },
          "statusId": { "type": "integer", "description": "New status id, from list_statuses." },
          "priority": {
            "type": "string",
            "enum": ["None", "Low", "Medium", "High", "Critical"],
            "description": "New priority. Use None to take a priority off the task."
          },
          "startDate": { "type": "string", "description": "New start date as YYYY-MM-DD." },
          "dueDate": { "type": "string", "description": "New due date as YYYY-MM-DD." },
          "estimateType": {
            "type": "string",
            "enum": ["StoryPoints", "Hours", "TShirt"],
            "description": "Unit the estimate is measured in. Required when the task has no estimate yet."
          },
          "estimateValue": {
            "type": "number",
            "description": "Estimate in the unit above. T-shirt sizes are 1 to 5, from XS to XL."
          },
          "clear": {
            "type": "array",
            "items": { "type": "string", "enum": ["startDate", "dueDate"] },
            "description": "Dates to remove from the task. A date cannot be both cleared and set."
          },
          "assigneeIds": {
            "type": "array",
            "items": { "type": "string" },
            "description": "The complete set of assignee ids, from list_members. An empty array clears them."
          },
          "tags": {
            "type": "array",
            "items": { "type": "string" },
            "description": "The complete set of tag names, existing or proposed with propose_create_tag. An empty array clears them."
          },
          "boardGroupId": { "type": "integer", "description": "Board group (the column on a board) to move the task into." }
        }
        """);

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        var canRead = RequiredPermissions.All(permissions.Contains);
        var canChangeSomething = PermissionsByChange.Values.Any(permissions.Contains);

        return canRead && canChangeSomething;
    }

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        var required = new HashSet<string>(StringComparer.Ordinal);
        var changesFields = FieldArguments.Any(name => HasArgument(arguments, name));
        var changesAssignees = HasArgument(arguments, "assigneeIds");
        var changesTags = HasArgument(arguments, "tags");
        var changesBoardGroup = HasArgument(arguments, "boardGroupId");

        if (changesFields)
        {
            required.Add(PermissionsByChange[UpdateChange]);
        }

        if (changesAssignees)
        {
            required.Add(PermissionsByChange[AiTaskChangeDrafts.AssignChange]);
        }

        if (changesTags)
        {
            required.Add(PermissionsByChange[AiTaskChangeDrafts.TagsChange]);
        }

        if (changesBoardGroup)
        {
            required.Add(PermissionsByChange[AiTaskChangeDrafts.BoardGroupChange]);
        }

        return required;
    }

    public IReadOnlySet<string> GetChangePermissions(string changeName, JsonElement payload)
    {
        var isKnownChange = PermissionsByChange.TryGetValue(changeName, out var permission);
        var resolved = isKnownChange ? permission! : NetptunePermissions.Tasks.Update;

        return new HashSet<string>(StringComparer.Ordinal) { resolved };
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var target = await ResolveTarget(arguments, cancellationToken);

        if (target.Error is not null)
        {
            return AiToolExecution.Failed(target.Error);
        }

        var resolved = target.Target!;
        var task = resolved.Task;
        var drafts = new List<AiChangeDraft>();

        if (task is not null)
        {
            var fieldResult = await ProposeFields(task, arguments, cancellationToken);
            var changesAssignees = HasArgument(arguments, "assigneeIds");
            var assigneeResult = changesAssignees
                ? await AiTaskChangeDrafts.Assignees(Mediator, task, arguments, cancellationToken)
                : AiTaskDraftResult.Unchanged;

            var boardGroupId = AiToolSchema.GetInt(arguments, "boardGroupId");
            var boardGroupResult = boardGroupId.HasValue
                ? await AiTaskChangeDrafts.BoardGroup(Mediator, task, boardGroupId.Value, cancellationToken)
                : AiTaskDraftResult.Unchanged;

            var taskError = fieldResult.Error ?? assigneeResult.Error ?? boardGroupResult.Error;

            if (taskError is not null)
            {
                return AiToolExecution.Failed(taskError);
            }

            AddDraft(drafts, fieldResult);
            AddDraft(drafts, assigneeResult);
            AddDraft(drafts, boardGroupResult);
        }

        var changesTags = HasArgument(arguments, "tags");

        if (changesTags)
        {
            var tagResult = await AiTaskChangeDrafts.Tags(Mediator, ChangeSet, resolved, arguments, cancellationToken);

            if (tagResult.Error is not null)
            {
                return AiToolExecution.Failed(tagResult.Error);
            }

            AddDraft(drafts, tagResult);
        }

        if (drafts.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this task.");
        }

        foreach (var draft in drafts)
        {
            ChangeSet.Add(draft);
        }

        return AiToolExecution.Success(
            $"Proposed updating “{resolved.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }

    private sealed record TargetResult(AiTaskTarget? Target, string? Error);

    private async Task<TargetResult> ResolveTarget(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskRef = AiPendingReference.Read(arguments, "taskRef");

        if (taskRef is not null)
        {
            return ResolvePendingTarget(arguments, taskRef);
        }

        var taskId = AiToolSchema.GetInt(arguments, "taskId");

        if (!taskId.HasValue)
        {
            return new TargetResult(null, "A taskId is required, or a taskRef for a task proposed in this change set.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return new TargetResult(null, $"Task {taskId} was not found in this workspace.");
        }

        return new TargetResult(new AiTaskTarget { Task = task, Name = task.Name }, null);
    }

    private TargetResult ResolvePendingTarget(JsonElement arguments, string taskRef)
    {
        var pending = AiPendingReference.Find(ChangeSet, taskRef, "task");

        if (pending is null)
        {
            return new TargetResult(null, AiPendingReference.Missing(taskRef, "task"));
        }

        var changesFields = FieldArguments.Any(name => HasArgument(arguments, name));
        var changesAssignees = HasArgument(arguments, "assigneeIds");
        var changesBoardGroup = HasArgument(arguments, "boardGroupId");
        var changesMoreThanTags = changesFields || changesAssignees || changesBoardGroup;

        if (changesMoreThanTags)
        {
            return new TargetResult(
                null,
                "A task proposed in this change set only takes tags here. Set everything else on propose_create_task.");
        }

        var target = new AiTaskTarget { RefKey = taskRef, Name = AiPendingReference.ProposedName(pending) };

        return new TargetResult(target, null);
    }

    private async Task<AiTaskDraftResult> ProposeFields(
        TaskViewModel task,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var cleared = ReadCleared(arguments);
        var unknownClear = cleared.FirstOrDefault(field => !ClearableFields.Contains(field));

        if (unknownClear is not null)
        {
            return AiTaskDraftResult.Failed($"“{unknownClear}” cannot be cleared. Only startDate and dueDate can.");
        }

        var fields = new List<AiChangeField>();
        var proposedDescription = AiToolSchema.GetString(arguments, "description");

        AddChangedField(fields, "name", task.Name, AiToolSchema.GetString(arguments, "name"));
        AddChangedField(fields, "description", task.Description, proposedDescription);

        var dateMessage = AddDateFields(fields, task, arguments, cleared);

        if (dateMessage is not null)
        {
            return AiTaskDraftResult.Failed(dateMessage);
        }

        var priorityMessage = AddPriorityField(fields, task, arguments);

        if (priorityMessage is not null)
        {
            return AiTaskDraftResult.Failed(priorityMessage);
        }

        var estimateMessage = AddEstimateField(fields, task, arguments);

        if (estimateMessage is not null)
        {
            return AiTaskDraftResult.Failed(estimateMessage);
        }

        var statusMessage = await AddStatusField(fields, task, arguments, cancellationToken);

        if (statusMessage is not null)
        {
            return AiTaskDraftResult.Failed(statusMessage);
        }

        if (fields.Count == 0)
        {
            return AiTaskDraftResult.Unchanged;
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        return AiTaskDraftResult.Proposed(new AiChangeDraft
        {
            ToolName = UpdateChange,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Update {changedNames} on “{task.Name}”",
            Fields = fields,
            Payload = CreateFieldPayload(task, arguments),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });
    }

    private static JsonDocument CreateFieldPayload(TaskViewModel task, JsonElement arguments)
    {
        var payload = new JsonObject { ["taskId"] = task.Id };
        var fieldArguments = arguments.EnumerateObject().Where(property => FieldArguments.Contains(property.Name));

        foreach (var property in fieldArguments)
        {
            payload[property.Name] = JsonNode.Parse(property.Value.GetRawText());
        }

        return JsonDocument.Parse(payload.ToJsonString());
    }

    private static void AddDraft(List<AiChangeDraft> drafts, AiTaskDraftResult result)
    {
        if (result.Draft is not null)
        {
            drafts.Add(result.Draft);
        }
    }

    private static bool HasArgument(JsonElement arguments, string name)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        return isObject && arguments.TryGetProperty(name, out var value) && value.ValueKind != JsonValueKind.Null;
    }

    private static List<string> ReadCleared(JsonElement arguments)
    {
        var isObject = arguments.ValueKind == JsonValueKind.Object;

        if (!isObject)
        {
            return [];
        }

        var hasProperty = arguments.TryGetProperty("clear", out var value) && value.ValueKind == JsonValueKind.Array;

        if (!hasProperty)
        {
            return [];
        }

        return value.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => item.GetString()!)
            .ToList();
    }

    private static string? AddDateFields(
        List<AiChangeField> fields,
        TaskViewModel task,
        JsonElement arguments,
        List<string> cleared)
    {
        var startMessage = AddDateField(fields, "startDate", task.StartDate, arguments, cleared);

        if (startMessage is not null)
        {
            return startMessage;
        }

        return AddDateField(fields, "dueDate", task.DueDate, arguments, cleared);
    }

    private static string? AddDateField(
        List<AiChangeField> fields,
        string name,
        DateOnly? before,
        JsonElement arguments,
        List<string> cleared)
    {
        var raw = AiToolSchema.GetString(arguments, name);
        var isCleared = cleared.Contains(name);
        var hasValue = !string.IsNullOrWhiteSpace(raw);

        if (isCleared && hasValue)
        {
            return $"“{name}” cannot be set and cleared in the same change.";
        }

        if (isCleared)
        {
            var wasSet = before.HasValue;

            if (wasSet)
            {
                fields.Add(AiChangeFields.Date(name, before, null));
            }

            return null;
        }

        if (!hasValue)
        {
            return null;
        }

        var isParsed = DateOnly.TryParse(raw, out var parsed);

        if (!isParsed)
        {
            return $"“{name}” must be a date in YYYY-MM-DD form.";
        }

        var isUnchanged = before == parsed;

        if (!isUnchanged)
        {
            fields.Add(AiChangeFields.Date(name, before, parsed));
        }

        return null;
    }

    private static string? AddPriorityField(List<AiChangeField> fields, TaskViewModel task, JsonElement arguments)
    {
        var raw = AiToolSchema.GetString(arguments, "priority");
        var hasValue = !string.IsNullOrWhiteSpace(raw);

        if (!hasValue)
        {
            return null;
        }

        var isParsed = Enum.TryParse<TaskPriority>(raw, true, out var priority);

        if (!isParsed)
        {
            return $"“{raw}” is not a priority. Use None, Low, Medium, High or Critical.";
        }

        var before = task.Priority ?? TaskPriority.None;

        AddChangedField(fields, "priority", before.ToString(), priority.ToString());

        return null;
    }

    private static string? AddEstimateField(List<AiChangeField> fields, TaskViewModel task, JsonElement arguments)
    {
        var rawType = AiToolSchema.GetString(arguments, "estimateType");
        var value = AiToolSchema.GetDecimal(arguments, "estimateValue");
        var hasType = !string.IsNullOrWhiteSpace(rawType);

        if (!hasType && !value.HasValue)
        {
            return null;
        }

        var isParsed = !hasType || Enum.TryParse<EstimateType>(rawType, true, out _);

        if (!isParsed)
        {
            return $"“{rawType}” is not an estimate unit. Use StoryPoints, Hours or TShirt.";
        }

        var type = hasType ? Enum.Parse<EstimateType>(rawType!, true) : task.EstimateType;

        if (!type.HasValue)
        {
            return "This task has no estimate unit yet, so estimateType is required alongside estimateValue.";
        }

        var resolved = value ?? task.EstimateValue;

        if (!resolved.HasValue)
        {
            return "An estimateValue is required to give this task an estimate.";
        }

        var isTShirt = type.Value == EstimateType.TShirt;
        var isOutOfRange = resolved.Value < SmallestTShirtSize || resolved.Value > LargestTShirtSize;

        if (isTShirt && isOutOfRange)
        {
            return "T-shirt estimates run from 1 (XS) to 5 (XL).";
        }

        var before = FormatEstimate(task.EstimateType, task.EstimateValue);

        AddChangedField(fields, "estimate", before, FormatEstimate(type, resolved));

        return null;
    }

    private static string? FormatEstimate(EstimateType? type, decimal? value)
    {
        var hasEstimate = type.HasValue && value.HasValue;

        if (!hasEstimate)
        {
            return null;
        }

        return $"{value!.Value:0.##} {type!.Value}";
    }

    private async Task<string?> AddStatusField(
        List<AiChangeField> fields,
        TaskViewModel task,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var statusId = AiToolSchema.GetInt(arguments, "statusId");

        if (!statusId.HasValue)
        {
            return null;
        }

        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
        var status = statuses?.FirstOrDefault(item => item.Id == statusId.Value);

        if (status is null)
        {
            return $"Status {statusId} is not in this workspace.";
        }

        var isUnchanged = task.StatusId == status.Id;

        if (isUnchanged)
        {
            return null;
        }

        fields.Add(AiChangeFields.Values(
            "status",
            AiChangeValueKind.Status,
            [AiChangeFields.Status(task.StatusId, task.StatusName, task.StatusColor)],
            [AiChangeFields.Status(status.Id, status.Name, status.Color)]));

        return null;
    }

    private static void AddChangedField(
        List<AiChangeField> fields,
        string name,
        string? before,
        string? after)
    {
        var hasValue = !string.IsNullOrWhiteSpace(after);

        if (!hasValue)
        {
            return;
        }

        var isUnchanged = string.Equals(before, after, StringComparison.Ordinal);

        if (isUnchanged)
        {
            return;
        }

        fields.Add(new AiChangeField { Name = name, Before = before, After = after });
    }
}
