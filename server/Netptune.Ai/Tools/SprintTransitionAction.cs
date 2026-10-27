using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Localization;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Sprints;
using Netptune.Handlers.Sprints.Queries;

namespace Netptune.Ai.Tools;

public sealed class SprintTransitionAction : IAiToolAction
{
    public const string StartChange = "propose_start_sprint";
    public const string CompleteChange = "propose_complete_sprint";
    public const string CancelChange = "propose_cancel_sprint";
    public const string DeleteChange = "propose_delete_sprint";

    public const string Start = "start";
    public const string Complete = "complete";
    public const string Cancel = "cancel";
    public const string Delete = "delete";

    private const int ActiveSprintLookupTake = 1;

    private static readonly string[] ClosingChanges = [CompleteChange, CancelChange];

    private static readonly IReadOnlySet<string> UpdatePermissions =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Update };

    private static readonly IReadOnlySet<string> ClosingPermissions =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Update, NetptunePermissions.Sprints.ManageTasks };

    private static readonly IReadOnlySet<string> DeletePermissions =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Delete };

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;
    private readonly string Transition;

    public SprintTransitionAction(IMediator mediator, IAiChangeSetBuilder changeSet, string transition)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
        Transition = transition;
    }

    public string ChangeName
    {
        get
        {
            return Transition switch
            {
                Start => StartChange,
                Complete => CompleteChange,
                Cancel => CancelChange,
                _ => DeleteChange,
            };
        }
    }

    public IReadOnlySet<string> RequiredPermissions
    {
        get
        {
            return Transition switch
            {
                Start => UpdatePermissions,
                Complete or Cancel => ClosingPermissions,
                _ => DeletePermissions,
            };
        }
    }

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

        var execution = Transition switch
        {
            Start => await ProposeStart(sprint, cancellationToken),
            Complete => ProposeComplete(sprint),
            Cancel => ProposeCancel(sprint),
            _ => ProposeDelete(sprint),
        };

        return execution;
    }

    private async Task<AiToolExecution> ProposeStart(SprintDetailViewModel sprint, CancellationToken cancellationToken)
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
            Before = LocalizedEnum.Label(SprintStatus.Planning),
            After = LocalizedEnum.Label(SprintStatus.Active),
        };

        return Propose(StartChange, sprint, Localized.Format(AppText.AiSummaryStartSprint, sprint.Name), [statusField]);
    }

    private AiToolExecution ProposeComplete(SprintDetailViewModel sprint)
    {
        var isActive = sprint.Status == SprintStatus.Active;

        if (!isActive)
        {
            return AiToolExecution.Failed(
                $"Sprint “{sprint.Name}” is {Describe(sprint.Status)} — only active sprints can be completed.");
        }

        var unfinishedCount = sprint.Tasks.Count(task => task.StatusCategory != StatusCategory.Done);
        var fields = new List<AiChangeField>
        {
            new()
            {
                Name = "status",
                Before = LocalizedEnum.Label(SprintStatus.Active),
                After = LocalizedEnum.Label(SprintStatus.Completed),
            },
        };

        var hasUnfinishedTasks = unfinishedCount > 0;

        if (hasUnfinishedTasks)
        {
            fields.Add(new AiChangeField { Name = "unfinishedTasks", After = unfinishedCount.ToString() });
        }

        return Propose(CompleteChange, sprint, Localized.Format(AppText.AiSummaryCompleteSprint, sprint.Name), fields);
    }

    private AiToolExecution ProposeCancel(SprintDetailViewModel sprint)
    {
        var isAlreadyCancelled = sprint.Status == SprintStatus.Cancelled;

        if (isAlreadyCancelled)
        {
            return AiToolExecution.Failed($"Sprint “{sprint.Name}” is already cancelled.");
        }

        var statusField = new AiChangeField
        {
            Name = "status",
            Before = LocalizedEnum.Label(sprint.Status),
            After = LocalizedEnum.Label(SprintStatus.Cancelled),
        };

        var summary = Localized.Format(AppText.AiSummaryCancelSprint, sprint.Name);

        return Propose(CancelChange, sprint, summary, [statusField]);
    }

    private AiToolExecution ProposeDelete(SprintDetailViewModel sprint)
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

        return Propose(DeleteChange, sprint, Localized.Format(AppText.AiSummaryDeleteSprint, sprint.Name), fields);
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
