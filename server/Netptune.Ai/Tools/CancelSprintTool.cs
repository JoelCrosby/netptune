using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class CancelSprintTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CancelSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public const string ToolName = "propose_cancel_sprint";

    public string Name => ToolName;

    public string Description =>
        "Propose cancelling a sprint. Its tasks stay attached to it, and a cancelled sprint can be deleted afterwards. "
        + "The sprint is not cancelled until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The id of the sprint to cancel." }
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

        var isAlreadyCancelled = sprint.Status == SprintStatus.Cancelled;

        if (isAlreadyCancelled)
        {
            return AiToolExecution.Failed($"Sprint “{sprint.Name}” is already cancelled.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = $"Cancel sprint “{sprint.Name}”",
            Fields =
            [
                new AiChangeField
                {
                    Name = "status",
                    Before = sprint.Status.ToString(),
                    After = SprintStatus.Cancelled.ToString(),
                },
            ],
            Payload = JsonSerializer.SerializeToDocument(new { sprintId = sprint.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed cancelling sprint “{sprint.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }
}
