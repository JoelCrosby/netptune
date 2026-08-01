using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

public sealed class CreateProjectTool : IAiTool
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public CreateProjectTool(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string Name => "propose_create_project";

    public string Description =>
        "Propose creating a project. The project is not created until the user reviews and applies the change.";

    public AiToolKind Kind => AiToolKind.Write;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Projects.Create };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "name": { "type": "string", "description": "The project name." },
          "description": { "type": "string", "description": "Optional project description." },
          "repositoryUrl": { "type": "string", "description": "Optional source repository url." }
        }
        """,
        "name");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return AiToolExecution.Failed("A project name is required.");
        }

        var projects = await Mediator.Send(new GetProjectsQuery(), cancellationToken);
        var existing = projects.FirstOrDefault(project =>
            string.Equals(project.Name, name, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            return AiToolExecution.Failed($"A project named \"{existing.Name}\" already exists.");
        }

        var description = AiToolSchema.GetString(arguments, "description");
        var repositoryUrl = AiToolSchema.GetString(arguments, "repositoryUrl");
        var fields = new List<AiChangeField> { new() { Name = "name", After = name } };

        AiToolSchema.AddOptionalField(fields, "description", description);
        AiToolSchema.AddOptionalField(fields, "repositoryUrl", repositoryUrl);

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Name,
            EntityType = "project",
            RefKey = ChangeSet.CreateRefKey(),
            Summary = $"Create project “{name}”",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed creating project \"{name}\". Nothing has been applied yet — the user must review and apply the change.");
    }
}
