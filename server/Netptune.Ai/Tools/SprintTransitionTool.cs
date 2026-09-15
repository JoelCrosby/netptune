using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Queries;

namespace Netptune.Ai.Tools;

public sealed class SprintTransitionTool : IAiTool
{
    public const string StartChange = "propose_start_sprint";
    public const string CompleteChange = "propose_complete_sprint";
    public const string CancelChange = "propose_cancel_sprint";
    public const string DeleteChange = "propose_delete_sprint";

    private const int ActiveSprintLookupTake = 1;

    private static readonly string[] ClosingChanges = [CompleteChange, CancelChange];

    private static readonly IReadOnlySet<string> UpdatePermissions =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Update };

    private static readonly IReadOnlySet<string> DeletePermissions =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Delete };

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public SprintTransitionTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_sprint_transition";

    public string Description =>
        "Propose moving a sprint through its lifecycle. "
        + "start makes a planning sprint the project's active sprint; a project has one active sprint, "
        + "so complete or cancel the running one first. "
        + "complete closes the active sprint, leaving unfinished tasks where they are, and it can no longer be edited. "
        + "cancel keeps its tasks attached and lets it be deleted afterwards. "
        + "delete removes a planning or cancelled sprint and sends its tasks back to the backlog.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Read };

    public IReadOnlyList<string> ProposedChanges { get; } = [StartChange, CompleteChange, CancelChange, DeleteChange];

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "sprintId": { "type": "integer", "description": "The id of the sprint." },
          "action": {
            "type": "string",
            "enum": ["start", "complete", "cancel", "delete"],
            "description": "What to do to the sprint."
          }
        }
        """,
        "sprintId",
        "action");

    public bool IsAvailable(IReadOnlySet<string> permissions)
    {
        var canRead = RequiredPermissions.All(permissions.Contains);
        var canUpdate = UpdatePermissions.All(permissions.Contains);
        var canDelete = DeletePermissions.All(permissions.Contains);

        return canRead && (canUpdate || canDelete);
    }

    public IReadOnlySet<string> GetRequiredPermissions(JsonElement arguments)
    {
        var action = AiToolSchema.GetString(arguments, "action");
        var isDelete = string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase);

        return isDelete ? DeletePermissions : UpdatePermissions;
    }

    public IReadOnlySet<string> GetChangePermissions(string changeName, JsonElement payload)
    {
        var isDelete = changeName == DeleteChange;

        return isDelete ? DeletePermissions : UpdatePermissions;
    }

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var sprintId = AiToolSchema.GetInt(arguments, "sprintId");

        if (!sprintId.HasValue)
        {
            return AiToolExecution.Failed("A sprintId is required.");
        }

        var action = AiToolSchema.GetString(arguments, "action")?.Trim().ToLowerInvariant();
        var isKnownAction = action is "start" or "complete" or "cancel" or "delete";

        if (!isKnownAction)
        {
            return AiToolExecution.Failed("An action of start, complete, cancel or delete is required.");
        }

        var sprint = await AiSprintLookup.Find(Mediator, sprintId.Value, cancellationToken);

        if (sprint is null)
        {
            return AiToolExecution.Failed($"Sprint {sprintId} is not in this workspace.");
        }

        var execution = action switch
        {
            "start" => await Start(sprint, cancellationToken),
            "complete" => Complete(sprint),
            "cancel" => Cancel(sprint),
            _ => Delete(sprint),
        };

        return execution;
    }

    private async Task<AiToolExecution> Start(SprintDetailViewModel sprint, CancellationToken cancellationToken)
    {
        var isPlanning = sprint.Status == SprintStatus.Planning;

        if (!isPlanning)
        {
            return AiToolExecution.Failed(
                $"Sprint “{sprint.Name}” is {Describe(sprint.Status)} — only planning sprints can be started.");
        }

        var activeSprint = await FindActiveSprint(sprint.ProjectId, cancellationToken);
        var isBlocked = activeSprint is not null && !IsBeingClosed(activeSprint.Id);

        if (isBlocked)
        {
            return AiToolExecution.Failed(
                $"Project {sprint.ProjectName} is already running sprint “{activeSprint!.Name}” ({activeSprint.Id}). "
                + "A project can only have one active sprint — propose completing or cancelling that one first.");
        }

        var statusField = new AiChangeField
        {
            Name = "status",
            Before = SprintStatus.Planning.ToString(),
            After = SprintStatus.Active.ToString(),
        };

        return Propose(StartChange, sprint, $"Start sprint “{sprint.Name}”", [statusField]);
    }

    private AiToolExecution Complete(SprintDetailViewModel sprint)
    {
        var isActive = sprint.Status == SprintStatus.Active;

        if (!isActive)
        {
            return AiToolExecution.Failed(
                $"Sprint “{sprint.Name}” is {Describe(sprint.Status)} — only active sprints can be completed.");
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

        return Propose(CompleteChange, sprint, $"Complete sprint “{sprint.Name}”", fields);
    }

    private AiToolExecution Cancel(SprintDetailViewModel sprint)
    {
        var isAlreadyCancelled = sprint.Status == SprintStatus.Cancelled;

        if (isAlreadyCancelled)
        {
            return AiToolExecution.Failed($"Sprint “{sprint.Name}” is already cancelled.");
        }

        var statusField = new AiChangeField
        {
            Name = "status",
            Before = sprint.Status.ToString(),
            After = SprintStatus.Cancelled.ToString(),
        };

        return Propose(CancelChange, sprint, $"Cancel sprint “{sprint.Name}”", [statusField]);
    }

    private AiToolExecution Delete(SprintDetailViewModel sprint)
    {
        var isDeletable = sprint.Status is SprintStatus.Planning or SprintStatus.Cancelled;

        if (!isDeletable)
        {
            return AiToolExecution.Failed(
                $"Sprint “{sprint.Name}” is {Describe(sprint.Status)} — only planning or cancelled sprints can be deleted.");
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

        return Propose(DeleteChange, sprint, $"Delete sprint “{sprint.Name}”", fields);
    }

    private AiToolExecution Propose(
        string changeName,
        SprintDetailViewModel sprint,
        string summary,
        List<AiChangeField> fields)
    {
        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = changeName,
            EntityType = "sprint",
            EntityId = sprint.Id,
            Summary = summary,
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { sprintId = sprint.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed: {summary}. Nothing has been applied yet — the user must review and apply the change.");
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
            change.EntityId == sprintId && ClosingChanges.Contains(change.ToolName));
    }

    private static string Describe(SprintStatus status)
    {
        return status.ToString().ToLowerInvariant();
    }
}
