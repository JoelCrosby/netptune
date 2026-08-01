using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class UpdateSprintTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_update_sprint";

    public string Description =>
        "Propose changing a sprint's name, goal, start date or end date. Use list_sprints to find sprint ids first. "
        + "The change is not applied until the user reviews and applies it.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The id of the sprint to change." },
          "name": { "type": "string", "description": "New sprint name." },
          "goal": { "type": "string", "description": "New sprint goal." },
          "startDate": { "type": "string", "description": "New first day of the sprint as YYYY-MM-DD." },
          "endDate": { "type": "string", "description": "New last day of the sprint as YYYY-MM-DD." }
        }
        """,
        "sprintId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (!sprintId.HasValue)
        {
            return AiToolExecution.Failed("A sprintId is required.");
        }

        var sprint = await AiSprintLookup.Find(Mediator, sprintId.Value, cancellationToken);

        if (sprint is null)
        {
            return AiToolExecution.Failed($"Sprint {sprintId} is not in this workspace.");
        }

        var isCompleted = sprint.Status == SprintStatus.Completed;

        if (isCompleted)
        {
            return AiToolExecution.Failed($"Sprint “{sprint.Name}” is completed and can no longer be edited.");
        }

        var startText = AiToolSchema.GetString(arguments, "startDate");
        var endText = AiToolSchema.GetString(arguments, "endDate");
        var startDate = ParseDate(startText);
        var endDate = ParseDate(endText);
        var hasUnreadableStart = startText is not null && startDate is null;
        var hasUnreadableEnd = endText is not null && endDate is null;

        if (hasUnreadableStart || hasUnreadableEnd)
        {
            return AiToolExecution.Failed("Sprint dates must be supplied as YYYY-MM-DD.");
        }

        var resultingStart = startDate ?? DateOnly.FromDateTime(sprint.StartDate);
        var resultingEnd = endDate ?? DateOnly.FromDateTime(sprint.EndDate);
        var endsBeforeItStarts = resultingEnd < resultingStart;

        if (endsBeforeItStarts)
        {
            return AiToolExecution.Failed("The sprint end date must not fall before its start date.");
        }

        var fields = new List<AiChangeField>();
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var goal = AiToolSchema.GetString(arguments, "goal");

        AddChangedField(fields, "name", sprint.Name, name);
        AddChangedField(fields, "goal", sprint.Goal, goal);
        AddChangedField(fields, "startDate", Format(sprint.StartDate), Format(startDate));
        AddChangedField(fields, "endDate", Format(sprint.EndDate), Format(endDate));

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this sprint.");
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = $"Update {changedNames} on sprint “{sprint.Name}”",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed updating sprint {sprint.Id}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static DateOnly? ParseDate(string? raw)
    {
        var isParsed = DateOnly.TryParse(raw, out var parsed);

        return isParsed ? parsed : null;
    }

    private static string Format(DateTime value)
    {
        return value.ToString(AiSprintLookup.DateFormat);
    }

    private static string? Format(DateOnly? value)
    {
        return value?.ToString(AiSprintLookup.DateFormat);
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
