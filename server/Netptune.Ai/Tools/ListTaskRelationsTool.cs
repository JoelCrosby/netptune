using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Relations.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListTaskRelationsTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListTaskRelationsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_task_relations";

    public string Description =>
        "Read the tasks a task is linked to, with the relation that connects them, such as blocks or relates to.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Tasks.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "systemId": { "type": "string", "description": "The task system id, such as NPT-42." }
        }
        """,
        "systemId");

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var systemId = AiToolSchema.GetString(arguments, "systemId")?.Trim();
        var hasSystemId = !string.IsNullOrWhiteSpace(systemId);

        if (!hasSystemId)
        {
            return AiToolExecution.Failed("A systemId is required.");
        }

        var relations = await Mediator.Send(new GetTaskRelationsQuery(systemId!), cancellationToken);

        if (relations is null)
        {
            return AiToolExecution.Failed($"Task {systemId} was not found in this workspace.");
        }

        var summaries = relations.Select(relation => new
        {
            id = relation.Id,
            label = relation.Label,
            relationTypeId = relation.RelationTypeId,
            relationType = relation.RelationTypeName,
            relatedTaskId = relation.RelatedTask.Id,
            relatedSystemId = relation.RelatedTask.SystemId,
            relatedName = relation.RelatedTask.Name,
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
