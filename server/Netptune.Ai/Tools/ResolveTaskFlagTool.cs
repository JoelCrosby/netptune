using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Flags.Queries;
using Netptune.Handlers.Tasks.Queries;

namespace Netptune.Ai.Tools;

public sealed class ResolveTaskFlagTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public ResolveTaskFlagTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_resolve_task_flag";

    public string Description =>
        "Propose clearing a flag raised on a task, either as resolved because the underlying problem is fixed, "
        + "or as dismissed because it does not apply. Flag ids come from get_task. "
        + "The flag is not cleared until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Flags.Resolve };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "taskId": { "type": "integer", "description": "The id of the flagged task." },
          "flagId": { "type": "integer", "description": "The id of the flag to clear, from get_task." },
          "resolution": {
            "type": "string",
            "enum": ["Resolved", "Dismissed"],
            "description": "Resolved when the problem is fixed, Dismissed when the flag does not apply."
          }
        }
        """,
        "taskId",
        "flagId",
        "resolution");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var taskId = AiToolSchema.GetInt(arguments, "taskId");
        var flagId = AiToolSchema.GetInt(arguments, "flagId");

        if (!taskId.HasValue || !flagId.HasValue)
        {
            return AiToolExecution.Failed("A taskId and flagId are required.");
        }

        var raw = AiToolSchema.GetString(arguments, "resolution");
        var isParsed = Enum.TryParse<FlagResolutionType>(raw, true, out var resolution);

        if (!isParsed)
        {
            return AiToolExecution.Failed($"“{raw}” is not a resolution. Use Resolved or Dismissed.");
        }

        var task = await Mediator.Send(new GetTaskQuery(taskId.Value), cancellationToken);

        if (task is null)
        {
            return AiToolExecution.Failed($"Task {taskId} was not found in this workspace.");
        }

        var response = await Mediator.Send(new GetTaskFlagsQuery(task.Id), cancellationToken);
        var flag = response.Payload?.FirstOrDefault(item => item.Id == flagId.Value);

        if (flag is null)
        {
            return AiToolExecution.Failed($"Task {task.SystemId} has no open flag with id {flagId}.");
        }

        var payload = new
        {
            taskId = task.Id,
            flagId = flag.Id,
            resolution = resolution.ToString(),
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            EntityId = task.Id,
            Summary = $"Clear flag “{flag.Name}” on “{task.Name}” as {resolution.ToString().ToLowerInvariant()}",
            Fields =
            [
                new AiChangeField { Name = "flag", Before = flag.Name, After = resolution.ToString() },
            ],
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed clearing flag “{flag.Name}” on {task.SystemId}. Nothing has been applied yet — the user must review and apply the change.");
    }
}
