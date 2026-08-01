using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Comments.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListTaskCommentsTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListTaskCommentsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_task_comments";

    public string Description => "Read the comments on a task, oldest first, with who wrote each one.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Comments.Read };

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

        var comments = await Mediator.Send(new GetCommentsForTaskQuery(systemId!), cancellationToken);

        if (comments is null)
        {
            return AiToolExecution.Failed($"Task {systemId} was not found in this workspace.");
        }

        var summaries = comments.Select(comment => new
        {
            id = comment.Id,
            author = comment.UserDisplayName,
            body = comment.Body,
            createdAt = comment.CreatedAt,
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
