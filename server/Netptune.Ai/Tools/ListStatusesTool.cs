using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Statuses.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListStatusesTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListStatusesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_statuses";

    public string Description => "List the task statuses available in the current workspace.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Statuses.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var statuses = await Mediator.Send(new GetStatusesQuery(new StatusFilter()), cancellationToken);

        if (statuses is null)
        {
            return AiToolExecution.Failed("Statuses could not be read.");
        }

        var summaries = statuses.Select(status => new
        {
            id = status.Id,
            name = status.Name,
            category = status.Category.ToString(),
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
