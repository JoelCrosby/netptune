using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Sprints.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListSprintsTool : IAiTool
{
    private const int DefaultTake = 25;

    private readonly IMediator Mediator;

    public ListSprintsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_sprints";

    public string Description => "List sprints in the workspace, optionally filtered to one project.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Sprints.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "projectId": { "type": "integer", "description": "Restrict results to a single project." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var projectId = AiToolSchema.GetInt(arguments, "projectId");
        var query = new GetSprintsQuery(projectId, [], DefaultTake);
        var sprints = await Mediator.Send(query, cancellationToken);
        var summaries = sprints.Select(sprint => new
        {
            id = sprint.Id,
            name = sprint.Name,
            status = sprint.Status.ToString(),
            projectId = sprint.ProjectId,
            startDate = sprint.StartDate,
            endDate = sprint.EndDate,
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
