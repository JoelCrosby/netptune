using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListRelationTypesTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListRelationTypesTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_relation_types";

    public string Description =>
        "List the relation types tasks can be linked with, such as blocks or relates to.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.RelationTypes.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken);

        if (relationTypes is null)
        {
            return AiToolExecution.Failed("Relation types could not be read.");
        }

        var summaries = relationTypes.Select(relationType => new
        {
            id = relationType.Id,
            name = relationType.Name,
            inverseName = relationType.InverseName,
            category = relationType.Category.ToString(),
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
