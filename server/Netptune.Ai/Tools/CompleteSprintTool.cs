using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class CompleteSprintTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CompleteSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_complete_sprint";

    public string Description =>
        "Propose completing the active sprint. Unfinished tasks stay where they are, and a completed sprint can no longer be edited. "
        + "The sprint is not completed until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The id of the sprint to complete." }
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

        var isActive = sprint.Status == SprintStatus.Active;

        if (!isActive)
        {
            return AiToolExecution.Failed(
                $"Sprint “{sprint.Name}” is {sprint.Status.ToString().ToLowerInvariant()} — only active sprints can be completed.");
        }

        var unfinishedCount = sprint.NewTaskCount + sprint.ActiveTaskCount;
        var fields = new List<AiChangeField>
        {
            new()
            {
                Name = "status",
                Before = SprintStatus.Active.ToString(),
                After = SprintStatus.Completed.ToString(),
            },
        };

        var hasUnfinishedTasks = unfinishedCount > 0;

        if (hasUnfinishedTasks)
        {
            fields.Add(new AiChangeField { Name = "unfinishedTasks", After = unfinishedCount.ToString() });
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = $"Complete sprint “{sprint.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { sprintId = sprint.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed completing sprint “{sprint.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }
}
