using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateProjectAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateProjectAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_update_project";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Projects.Update };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var projectId = AiToolSchema.GetInt(arguments, "projectId");

        if (!projectId.HasValue)
        {
            return AiToolExecution.Failed("A projectId is required.");
        }

        var projects = await Mediator.Send(new GetProjectsQuery(), cancellationToken);
        var project = projects.FirstOrDefault(item => item.Id == projectId.Value);

        if (project is null)
        {
            return AiToolExecution.Failed($"Project {projectId} is not in this workspace.");
        }

        var fields = new List<AiChangeField>();

        AddChangedField(fields, "name", project.Name, AiToolSchema.GetString(arguments, "name"));
        AddChangedField(fields, "description", project.Description, AiToolSchema.GetString(arguments, "description"));
        AddChangedField(fields, "repositoryUrl", project.RepositoryUrl, AiToolSchema.GetString(arguments, "repositoryUrl"));

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this project.");
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = "project",
            EntityId = project.Id,
            Summary = $"Update {changedNames} on {project.Name}",
            Fields = fields,
            Payload = JsonDocument.Parse(arguments.GetRawText()),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed updating project {project.Id}. Nothing has been applied yet — the user must review and apply the change.");
    }

    private static void AddChangedField(
        List<AiChangeField> fields,
        string name,
        string? before,
        string? after)
    {
        var hasValue = !string.IsNullOrWhiteSpace(after);

        if (!hasValue)
        {
            return;
        }

        var isUnchanged = string.Equals(before, after, StringComparison.Ordinal);

        if (isUnchanged)
        {
            return;
        }

        fields.Add(new AiChangeField { Name = name, Before = before, After = after });
    }
}
