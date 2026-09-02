using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateStatusTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateStatusTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_create_status";

    public string Description =>
        "Propose creating a task status for the workspace. The status is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Statuses.Manage };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The status name." },
          "category": {
            "type": "string",
            "description": "Which stage the status belongs to.",
            "enum": ["New", "Backlog", "Todo", "Active", "Done", "Inactive"]
          },
          "description": { "type": "string", "description": "Optional status description." },
          "color": { "type": "string", "description": "Optional colour, as a hex value or a named colour." }
        }
        """,
        "name",
        "category");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A status name is required.");
        }

        var rawCategory = AiToolSchema.GetString(arguments, "category");
        var isKnownCategory = Enum.TryParse<StatusCategory>(rawCategory, true, out var category);

        if (!isKnownCategory)
        {
            return AiToolExecution.Failed(
                "A category of New, Backlog, Todo, Active, Done or Inactive is required.");
        }

        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);
        var existing = statuses?.FirstOrDefault(status =>
            string.Equals(status.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return AiToolExecution.Failed($"A status named \"{existing.Name}\" already exists.");
        }

        var refKey = ChangeSet.CreateRefKey();
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "category", After = category.ToString() },
        };

        AiToolSchema.AddOptionalField(fields, "description", AiToolSchema.GetString(arguments, "description"));
        AiToolSchema.AddOptionalField(fields, "color", AiToolSchema.GetString(arguments, "color"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "status",
            RefKey = refKey,
            Summary = $"Create status “{name}” in {category}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating status \"{name}\" as {refKey}. Nothing has been applied yet — the user must review and apply the change.");
    }
}
