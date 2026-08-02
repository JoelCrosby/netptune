using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class DeleteSprintTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_delete_sprint";

    public string Description =>
        "Propose deleting a planning or cancelled sprint. Its tasks are sent back to the backlog rather than deleted. "
        + "The sprint is not deleted until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Delete };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The id of the sprint to delete." }
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

        var isDeletable = sprint.Status is SprintStatus.Planning or SprintStatus.Cancelled;

        if (!isDeletable)
        {
            return AiToolExecution.Failed(
                $"Sprint “{sprint.Name}” is {sprint.Status.ToString().ToLowerInvariant()} — only planning or cancelled sprints can be deleted.");
        }

        var fields = new List<AiChangeField>
        {
            AiChangeFields.Values(
                "sprint",
                AiChangeValueKind.Sprint,
                [AiChangeFields.Sprint(sprint.Id, sprint.Name)],
                []),
        };

        var hasTasks = sprint.TaskCount > 0;

        if (hasTasks)
        {
            fields.Add(new AiChangeField { Name = "tasksReturnedToBacklog", After = sprint.TaskCount.ToString() });
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = $"Delete sprint “{sprint.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { sprintId = sprint.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting sprint “{sprint.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }
}
