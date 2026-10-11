using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

public sealed class DeleteProjectAction : IAiToolAction
{
    public const string Change = "propose_delete_project";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteProjectAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => Change;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Projects.Delete };

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

        var fields = new List<AiChangeField> { AiChangeFields.Text("project", project.Name, null) };

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = Change,
            EntityType = "project",
            EntityId = project.Id,
            Summary = $"Delete project “{project.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { projectId = project.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting project {project.Id}. "
            + "Nothing has been deleted yet — the user must review and apply the change.");
    }
}
