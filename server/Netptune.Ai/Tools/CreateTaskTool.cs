using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateTaskTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateTaskTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_create_task";

    public string Description =>
        "Propose creating a task. The task is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The task name." },
          "projectId": { "type": "integer", "description": "The project the task belongs to." },
          "description": { "type": "string", "description": "Optional task description." },
          "dueDate": { "type": "string", "description": "Optional due date as YYYY-MM-DD." }
        }
        """,
        "name",
        "projectId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var projectId = AiToolSchema.GetInt(arguments, "projectId");
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName || !projectId.HasValue)
        {
            return AiToolExecution.Failed("A task name and projectId are required.");
        }

        var projects = await Mediator.Send(new GetProjectsQuery(), cancellationToken);
        var project = projects.FirstOrDefault(item => item.Id == projectId.Value);

        if (project is null)
        {
            return AiToolExecution.Failed($"Project {projectId} is not in this workspace.");
        }

        var description = AiToolSchema.GetString(arguments, "description");
        var dueDate = AiToolSchema.GetString(arguments, "dueDate");
        var refKey = ChangeSet.CreateRefKey();
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "project", After = project.Name },
        };

        AddOptionalField(fields, "description", description);
        AddOptionalField(fields, "dueDate", dueDate);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "task",
            RefKey = refKey,
            Summary = $"Create task “{name}” in {project.Name}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating task \"{name}\" as {refKey}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static void AddOptionalField(List<AiChangeField> fields, string name, string? value)
    {
        var hasValue = !string.IsNullOrWhiteSpace(value);

        if (!hasValue)
        {
            return;
        }

        fields.Add(new AiChangeField { Name = name, After = value });
    }
}
