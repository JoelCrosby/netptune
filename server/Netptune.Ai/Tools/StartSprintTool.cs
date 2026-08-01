using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Queries;

namespace Netptune.Ai.Tools;

public sealed class StartSprintTool : IAiTool
{
    private const int ActiveSprintLookupTake = 1;

    private static readonly string[] ClosingTools =
        [CompleteSprintTool.ToolName, CancelSprintTool.ToolName];

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public StartSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_start_sprint";

    public string Description =>
        "Propose starting a sprint that is still in planning, making it the project's active sprint. "
        + "A project can only have one active sprint, so the sprint running now must be completed or cancelled first. "
        + "The sprint is not started until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Update };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The id of the sprint to start." }
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

        var isPlanning = sprint.Status == SprintStatus.Planning;

        if (!isPlanning)
        {
            return AiToolExecution.Failed(
                $"Sprint “{sprint.Name}” is {sprint.Status.ToString().ToLowerInvariant()} — only planning sprints can be started.");
        }

        var activeSprint = await FindActiveSprint(sprint.ProjectId, cancellationToken);
        var isBlocked = activeSprint is not null && !IsBeingClosed(activeSprint.Id);

        if (isBlocked)
        {
            return AiToolExecution.Failed(
                $"Project {sprint.ProjectName} is already running sprint “{activeSprint!.Name}” ({activeSprint.Id}). "
                + "A project can only have one active sprint — propose completing or cancelling that one first.");
        }

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = $"Start sprint “{sprint.Name}”",
            Fields =
            [
                new AiChangeField
                {
                    Name = "status",
                    Before = SprintStatus.Planning.ToString(),
                    After = SprintStatus.Active.ToString(),
                },
            ],
            Payload = JsonSerializer.SerializeToDocument(new { sprintId = sprint.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed starting sprint “{sprint.Name}”. Nothing has been applied yet — the user must review and apply the change.");
    }

    private async Task<SprintViewModel?> FindActiveSprint(int projectId, CancellationToken cancellationToken)
    {
        var query = new GetSprintsQuery(projectId, [SprintStatus.Active], ActiveSprintLookupTake);
        var sprints = await Mediator.Send(query, cancellationToken);

        return sprints.FirstOrDefault();
    }

    private bool IsBeingClosed(int sprintId)
    {
        return ChangeSet.Changes.Any(change =>
            change.EntityId == sprintId && ClosingTools.Contains(change.ToolName));
    }
}
