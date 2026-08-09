using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.RelationTypes;
using Netptune.Handlers.RelationTypes.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListRelationsTool : IAiTool
{
    private const int DefaultTake = 25;
    private const int MaxTake = 100;

    private readonly IMediator Mediator;

    public ListRelationsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_relations";

    public string Description =>
        "List the task links across the whole workspace, such as everything that blocks something else, "
        + "optionally narrowed to one relation type. Use list_task_relations for the links on a single task.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        NetptunePermissions.RelationTypes.Read,
        NetptunePermissions.Tasks.Read,
    };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "relationTypeId": { "type": "integer", "description": "Restrict to one relation type, from list_relation_types." },
          "relationType": { "type": "string", "description": "Relation type name or key, instead of relationTypeId." },
          "take": { "type": "integer", "description": "How many relations to return. Defaults to 25." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var requestedTake = AiToolSchema.GetInt(arguments, "take") ?? DefaultTake;
        var take = Math.Clamp(requestedTake, 1, MaxTake);
        var relationTypes = await Mediator.Send(new GetRelationTypesQuery(), cancellationToken);

        if (relationTypes is null)
        {
            return AiToolExecution.Failed("Relation types could not be read.");
        }

        var selection = SelectRelationTypes(arguments, relationTypes);

        if (selection.Error is not null)
        {
            return AiToolExecution.Failed(selection.Error);
        }

        var relations = new List<object>(take);
        var totalCount = 0;

        foreach (var relationType in selection.RelationTypes)
        {
            totalCount += relationType.RelationCount;

            var remaining = take - relations.Count;
            var isFull = remaining <= 0;

            if (isFull)
            {
                continue;
            }

            var page = new PageRequest { Page = 1, PageSize = remaining };
            var result = await Mediator.Send(new GetRelationsForTypeQuery(relationType.Id, page), cancellationToken);

            if (result is null)
            {
                continue;
            }

            var summaries = result.Items.Select(relation => new
            {
                id = relation.Id,
                relationTypeId = relationType.Id,
                relationType = relationType.Name,
                inverseName = relationType.InverseName,
                sourceSystemId = relation.SourceTask.SystemId,
                sourceName = relation.SourceTask.Name,
                sourceStatus = relation.SourceTask.StatusName,
                sourceIsArchived = relation.SourceTask.IsArchived,
                targetSystemId = relation.TargetTask.SystemId,
                targetName = relation.TargetTask.Name,
                targetStatus = relation.TargetTask.StatusName,
                targetIsArchived = relation.TargetTask.IsArchived,
            });

            relations.AddRange(summaries);
        }

        var summary = new
        {
            totalCount,
            returnedCount = relations.Count,
            relations,
        };

        var content = JsonSerializer.Serialize(summary);

        return AiToolExecution.Success(content);
    }

    private sealed record RelationTypeSelection
    {
        public IReadOnlyList<RelationTypeViewModel> RelationTypes { get; init; } = [];

        public string? Error { get; init; }
    }

    private static RelationTypeSelection SelectRelationTypes(
        JsonElement arguments,
        IReadOnlyList<RelationTypeViewModel> relationTypes)
    {
        var relationTypeId = AiToolSchema.GetInt(arguments, "relationTypeId");

        if (relationTypeId.HasValue)
        {
            var byId = relationTypes.FirstOrDefault(relationType => relationType.Id == relationTypeId.Value);

            if (byId is null)
            {
                return new RelationTypeSelection { Error = $"Relation type {relationTypeId} is not in this workspace." };
            }

            return new RelationTypeSelection { RelationTypes = [byId] };
        }

        var name = AiToolSchema.GetString(arguments, "relationType")?.Trim();
        var hasName = !string.IsNullOrWhiteSpace(name);

        if (!hasName)
        {
            return new RelationTypeSelection { RelationTypes = relationTypes };
        }

        var byName = relationTypes.FirstOrDefault(relationType =>
            string.Equals(relationType.Name, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(relationType.Key, name, StringComparison.OrdinalIgnoreCase)
            || string.Equals(relationType.InverseName, name, StringComparison.OrdinalIgnoreCase));

        if (byName is null)
        {
            var available = string.Join(", ", relationTypes.Select(relationType => relationType.Name));

            var error = $"Relation type “{name}” is not in this workspace. Available: {available}.";

            return new RelationTypeSelection { Error = error };
        }

        return new RelationTypeSelection { RelationTypes = [byName] };
    }
}
