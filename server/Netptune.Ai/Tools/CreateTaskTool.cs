using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;
using Netptune.Handlers.Statuses.Queries;
using Netptune.Handlers.Users.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateTaskTool : IAiTool
{
    private const int MemberLookupTake = 200;

    private static readonly string[] DateFields = ["startDate", "dueDate"];

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateTaskTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_create_task";

    public string Description =>
        "Propose creating a task, fully formed: assignee, sprint, board group, status, priority, estimate and dates "
        + "can all be set here rather than in a second change. Tags may be set here too. "
        + "The task is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Create };

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement payload)
    {
        var tags = AiTagVocabulary.ReadRequested(payload);
        var hasTags = tags.Count > 0;

        if (!hasTags)
        {
            return RequiredPermissions;
        }

        return new HashSet<string>(RequiredPermissions, StringComparer.Ordinal) { NetptunePermissions.Tags.Assign };
    }

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The task name." },
          "projectId": { "type": "integer", "description": "The project the task belongs to." },
          "projectRef": { "type": "string", "description": "Handle of a project proposed earlier in this change set, instead of projectId." },
          "description": { "type": "string", "description": "Optional task description." },
          "statusId": { "type": "integer", "description": "Status id, from list_statuses. Defaults to the project's first status." },
          "assigneeId": { "type": "string", "description": "Workspace user id to assign, from list_members." },
          "sprintId": { "type": "integer", "description": "Sprint to put the task in, from list_sprints. Must belong to the same project." },
          "sprintRef": { "type": "string", "description": "Handle of a sprint proposed earlier in this change set, instead of sprintId." },
          "boardGroupId": { "type": "integer", "description": "Board group to place the task in, from list_board_groups." },
          "tags": {
            "type": "array",
            "items": { "type": "string" },
            "description": "Tag names for the new task, from list_tags or proposed with propose_create_tag."
          },
          "priority": {
            "type": "string",
            "enum": ["None", "Low", "Medium", "High", "Critical"],
            "description": "Priority to start the task at."
          },
          "estimateType": {
            "type": "string",
            "enum": ["StoryPoints", "Hours", "TShirt"],
            "description": "Unit the estimate is measured in. Required alongside estimateValue."
          },
          "estimateValue": { "type": "number", "description": "Estimate in the unit above. T-shirt sizes are 1 to 5, from XS to XL." },
          "startDate": { "type": "string", "description": "Optional start date as YYYY-MM-DD." },
          "dueDate": { "type": "string", "description": "Optional due date as YYYY-MM-DD." }
        }
        """,
        "name");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A task name is required.");
        }

        var parent = await AiParentLookup.Project(Mediator, ChangeSet, arguments, cancellationToken);

        if (parent.Error is not null)
        {
            return AiToolExecution.Failed(parent.Error);
        }

        var project = parent.Parent!;
        var description = AiToolSchema.GetString(arguments, "description");
        var refKey = ChangeSet.CreateRefKey();
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "project", After = project.Name },
        };

        AiToolSchema.AddOptionalField(fields, "description", description);

        var placement = await ReadPlacement(project, arguments, cancellationToken);

        if (placement.Error is not null)
        {
            return AiToolExecution.Failed(placement.Error);
        }

        fields.AddRange(placement.Fields);

        var tagError = await AddTagField(fields, arguments, cancellationToken);

        if (tagError is not null)
        {
            return AiToolExecution.Failed(tagError);
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            RefKey = refKey,
            Summary = $"Create task “{name}” in {project.Name}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating task \"{name}\" as {refKey}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private async Task<string?> AddTagField(
        List<AiChangeField> fields,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var tags = AiTagVocabulary.ReadRequested(arguments);
        var hasTags = tags.Count > 0;

        if (!hasTags)
        {
            return null;
        }

        var knownNames = await AiTagVocabulary.Read(Mediator, ChangeSet, cancellationToken);

        if (knownNames is null)
        {
            return "Workspace tags could not be read.";
        }

        var unknownError = AiTagVocabulary.FindUnknown(tags, knownNames);

        if (unknownError is not null)
        {
            return unknownError;
        }

        fields.Add(AiChangeFields.Values(
            "tags",
            AiChangeValueKind.Tag,
            [],
            tags.Select(AiChangeFields.Tag)));

        return null;
    }

    private sealed record TaskPlacement(List<AiChangeField> Fields, string? Error)
    {
        public static TaskPlacement Failed(string error)
        {
            return new TaskPlacement([], error);
        }
    }

    private async Task<TaskPlacement> ReadPlacement(
        AiParent project,
        JsonElement arguments,
        CancellationToken cancellationToken)
    {
        var fields = new List<AiChangeField>();
        var statusId = AiToolSchema.GetInt(arguments, "statusId");

        if (statusId.HasValue)
        {
            var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
            var status = statuses?.FirstOrDefault(item => item.Id == statusId.Value);

            if (status is null)
            {
                return TaskPlacement.Failed($"Status {statusId} is not in this workspace.");
            }

            fields.Add(AiChangeFields.Values(
                "status",
                AiChangeValueKind.Status,
                [],
                [AiChangeFields.Status(status.Id, status.Name, status.Color)]));
        }

        var assigneeId = AiToolSchema.GetString(arguments, "assigneeId");
        var hasAssignee = !string.IsNullOrWhiteSpace(assigneeId);

        if (hasAssignee)
        {
            var filter = new AssigneeFilter { Page = 1, PageSize = MemberLookupTake };
            var result = await Mediator.Send(new GetAssigneesQuery(filter), cancellationToken);
            var member = result.Payload?.Items.FirstOrDefault(item =>
                string.Equals(item.Id, assigneeId, StringComparison.Ordinal));

            if (member is null)
            {
                return TaskPlacement.Failed($"User {assigneeId} is not a member of this workspace.");
            }

            fields.Add(AiChangeFields.Values(
                "assignee",
                AiChangeValueKind.User,
                [],
                [AiChangeFields.User(member.Id, member.DisplayName, member.PictureUrl)]));
        }

        var sprintRef = AiPendingReference.Read(arguments, "sprintRef");

        if (sprintRef is not null)
        {
            var pendingSprint = AiPendingReference.Find(ChangeSet, sprintRef, "sprint");

            if (pendingSprint is null)
            {
                return TaskPlacement.Failed(AiPendingReference.Missing(sprintRef, "sprint"));
            }

            fields.Add(AiChangeFields.Values(
                "sprint",
                AiChangeValueKind.Sprint,
                [],
                [AiChangeFields.Sprint(null, pendingSprint.Summary)]));
        }

        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (sprintId.HasValue)
        {
            // An existing sprint belongs to an existing project, so it can never belong to one
            // this change set has not created yet.
            if (project.IsPending)
            {
                return TaskPlacement.Failed(
                    "A task in a project proposed in this change set cannot join an existing sprint. "
                    + "Propose the sprint with propose_create_sprint and pass its handle as sprintRef.");
            }

            var sprint = await AiSprintLookup.Find(Mediator, sprintId.Value, cancellationToken);

            if (sprint is null)
            {
                return TaskPlacement.Failed($"Sprint {sprintId} is not in this workspace.");
            }

            var belongsToProject = sprint.ProjectId == project.Id;

            if (!belongsToProject)
            {
                return TaskPlacement.Failed($"Sprint “{sprint.Name}” belongs to a different project.");
            }

            fields.Add(AiChangeFields.Values(
                "sprint",
                AiChangeValueKind.Sprint,
                [],
                [AiChangeFields.Sprint(sprint.Id, sprint.Name)]));
        }

        var boardGroupId = AiToolSchema.GetInt(arguments, "boardGroupId");
        var isBoardGroupUnreachable = boardGroupId.HasValue && project.IsPending;

        if (isBoardGroupUnreachable)
        {
            return TaskPlacement.Failed(
                "A task in a project proposed in this change set cannot join an existing board group. "
                + "Leave boardGroupId unset and it lands in the new project's default board.");
        }

        var scheduleError = AddScheduleFields(fields, arguments);

        if (scheduleError is not null)
        {
            return TaskPlacement.Failed(scheduleError);
        }

        return new TaskPlacement(fields, null);
    }

    private static string? AddScheduleFields(List<AiChangeField> fields, JsonElement arguments)
    {
        var priority = AiToolSchema.GetString(arguments, "priority");
        var hasPriority = !string.IsNullOrWhiteSpace(priority);
        var isPriorityKnown = !hasPriority || Enum.TryParse<TaskPriority>(priority, true, out _);

        if (!isPriorityKnown)
        {
            return $"“{priority}” is not a priority. Use None, Low, Medium, High or Critical.";
        }

        AiToolSchema.AddOptionalField(fields, "priority", priority);

        var estimateType = AiToolSchema.GetString(arguments, "estimateType");
        var estimateValue = AiToolSchema.GetDecimal(arguments, "estimateValue");
        var hasEstimateType = !string.IsNullOrWhiteSpace(estimateType);
        var isEstimateKnown = !hasEstimateType || Enum.TryParse<EstimateType>(estimateType, true, out _);

        if (!isEstimateKnown)
        {
            return $"“{estimateType}” is not an estimate unit. Use StoryPoints, Hours or TShirt.";
        }

        var isEstimatePaired = hasEstimateType == estimateValue.HasValue;

        if (!isEstimatePaired)
        {
            return "An estimate needs both estimateType and estimateValue.";
        }

        if (estimateValue.HasValue)
        {
            fields.Add(new AiChangeField { Name = "estimate", After = $"{estimateValue.Value:0.##} {estimateType}" });
        }

        return AddDateFields(fields, arguments);
    }

    private static string? AddDateFields(List<AiChangeField> fields, JsonElement arguments)
    {
        foreach (var name in DateFields)
        {
            var raw = AiToolSchema.GetString(arguments, name);
            var hasValue = !string.IsNullOrWhiteSpace(raw);

            if (!hasValue)
            {
                continue;
            }

            var isParsed = DateOnly.TryParse(raw, out var parsed);

            if (!isParsed)
            {
                return $"“{name}” must be a date in YYYY-MM-DD form.";
            }

            fields.Add(AiChangeFields.Date(name, null, parsed));
        }

        return null;
    }
}
