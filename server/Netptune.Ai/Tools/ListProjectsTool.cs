using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Projects.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListProjectsTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListProjectsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_projects";

    public string Description => "List the projects in the current workspace, with their id and name.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Projects.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var projects = await Mediator.Send(new GetProjectsQuery(), cancellationToken);
        var summaries = projects.Select(project => new
        {
            id = project.Id,
            name = project.Name,
            key = project.Key,
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
