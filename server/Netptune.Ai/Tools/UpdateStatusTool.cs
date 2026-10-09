using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateStatusTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateStatusTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_update_status";

    public string Description =>
        "Propose renaming a workspace task status or changing its category, colour or description. "
        + "Fields that are not passed keep their current value.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Statuses.Manage };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "statusId": { "type": "integer", "description": "The id of the status to change, from list_records." },
          "name": { "type": "string", "description": "New status name." },
          "category": {
            "type": "string",
            "description": "New stage the status belongs to.",
            "enum": ["New", "Backlog", "Todo", "Active", "Done", "Inactive"]
          },
          "description": { "type": "string", "description": "New status description." },
          "color": { "type": "string", "description": "New colour, as a hex value or a named colour." }
        }
        """,
        "statusId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var statusId = AiToolSchema.GetInt(arguments, "statusId");

        if (!statusId.HasValue)
        {
            return AiToolExecution.Failed("A statusId is required.");
        }

        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken) ?? [];
        var status = statuses.FirstOrDefault(item => item.Id == statusId.Value);

        if (status is null)
        {
            return AiToolExecution.Failed($"Status {statusId} is not in this workspace.");
        }

        var fields = new List<AiChangeField>();
        var payload = new Dictionary<string, object> { ["statusId"] = status.Id };
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var isRenamed = !string.IsNullOrWhiteSpace(name) && !string.Equals(name, status.Name, StringComparison.Ordinal);

        if (isRenamed)
        {
            var key = name!.ToUrlSlug();
            var clash = statuses.FirstOrDefault(item => item.Id != status.Id && item.Key == key);

            if (clash is not null)
            {
                return AiToolExecution.Failed($"A status named \"{clash.Name}\" already exists.");
            }

            fields.Add(AiChangeFields.Text("name", status.Name, name));
            payload["name"] = name;
        }

        var rawCategory = AiToolSchema.GetString(arguments, "category");
        var hasCategory = !string.IsNullOrWhiteSpace(rawCategory);

        if (hasCategory)
        {
            var isKnownCategory = Enum.TryParse<StatusCategory>(rawCategory, true, out var category);

            if (!isKnownCategory)
            {
                return AiToolExecution.Failed("The category must be New, Backlog, Todo, Active, Done or Inactive.");
            }

            if (category != status.Category)
            {
                fields.Add(AiChangeFields.Text("category", status.Category.ToString(), category.ToString()));
                payload["category"] = category.ToString();
            }
        }

        AddChangedField(fields, payload, "description", status.Description, AiToolSchema.GetString(arguments, "description"));
        AddChangedField(fields, payload, "color", status.Color, AiToolSchema.GetString(arguments, "color"));

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this status.");
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "status",
            EntityId = status.Id,
            Summary = $"Update {changedNames} on status “{status.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed updating status {status.Id}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private static void AddChangedField(
        List<AiChangeField> fields,
        Dictionary<string, object> payload,
        string name,
        string? before,
        string? after)
    {
        var value = after?.Trim();
        var hasValue = !string.IsNullOrWhiteSpace(value);

        if (!hasValue)
        {
            return;
        }

        var isUnchanged = string.Equals(before, value, StringComparison.Ordinal);

        if (isUnchanged)
        {
            return;
        }

        fields.Add(AiChangeFields.Text(name, before, value));
        payload[name] = value!;
    }
}
