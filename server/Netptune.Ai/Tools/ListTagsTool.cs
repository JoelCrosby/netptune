using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Tags.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListTagsTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListTagsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_tags";

    public string Description => "List the tags available in the current workspace.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tags.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var tags = await Mediator.Send(new GetTagsForWorkspaceQuery(), cancellationToken);

        if (tags is null)
        {
            return AiToolExecution.Failed("Tags could not be read.");
        }

        var names = tags.Select(tag => tag.Name);
        var content = JsonSerializer.Serialize(names);

        return AiToolExecution.Success(content);
    }
}
