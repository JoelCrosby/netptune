using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateSprintTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateSprintTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_create_sprint";

    public string Description =>
        "Propose creating a sprint in a project. The sprint is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The sprint name." },
          "projectId": { "type": "integer", "description": "The project the sprint belongs to." },
          "startDate": { "type": "string", "description": "First day of the sprint as YYYY-MM-DD." },
          "endDate": { "type": "string", "description": "Last day of the sprint as YYYY-MM-DD." },
          "goal": { "type": "string", "description": "Optional sprint goal." }
        }
        """,
        "name",
        "projectId",
        "startDate",
        "endDate");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var projectId = AiToolSchema.GetInt(arguments, "projectId");
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName || !projectId.HasValue)
        {
            return AiToolExecution.Failed("A sprint name and projectId are required.");
        }

        var startDate = ReadDate(arguments, "startDate");
        var endDate = ReadDate(arguments, "endDate");

        if (startDate is null || endDate is null)
        {
            return AiToolExecution.Failed("A startDate and endDate as YYYY-MM-DD are required.");
        }

        var endsBeforeItStarts = endDate < startDate;

        if (endsBeforeItStarts)
        {
            return AiToolExecution.Failed("The sprint end date must not fall before its start date.");
        }

        var projects = await Mediator.Send(new GetProjectsQuery(), cancellationToken);
        var project = projects.FirstOrDefault(item => item.Id == projectId.Value);

        if (project is null)
        {
            return AiToolExecution.Failed($"Project {projectId} is not in this workspace.");
        }

        var goal = AiToolSchema.GetString(arguments, "goal");
        var fields = new List<AiChangeField>
        {
            new() { Name = "name", After = name },
            new() { Name = "project", After = project.Name },
            new() { Name = "startDate", After = startDate.Value.ToString("yyyy-MM-dd") },
            new() { Name = "endDate", After = endDate.Value.ToString("yyyy-MM-dd") },
        };

        AiToolSchema.AddOptionalField(fields, "goal", goal);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "sprint",
            RefKey = ChangeSet.CreateRefKey(),
            Summary = $"Create sprint “{name}” in {project.Name}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating sprint \"{name}\". Nothing has been applied yet — the user must review and apply the change.");
    }

    private static DateOnly? ReadDate(JsonElement arguments, string name)
    {
        var raw = AiToolSchema.GetString(arguments, name);
        var isParsed = DateOnly.TryParse(raw, out var parsed);

        return isParsed ? parsed : null;
    }
}
