using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Requests;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Boards.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListBoardsTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListBoardsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_boards";

    public string Description =>
        "List the boards in the current workspace, with their id, name, identifier and project. "
        + "Returns at most 100 boards; pass search to narrow a large workspace.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Boards.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Object(
        """
        {
          "search": { "type": "string", "description": "Optional fragment of a board name, identifier or project name." }
        }
        """);

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var filter = new BoardFilter
        {
            Search = AiToolSchema.GetString(arguments, "search"),
            Page = 1,
            PageSize = PaginationDefaults.MaxPageSize,
        };

        var result = await Mediator.Send(new GetBoardsInWorkspaceQuery(filter), cancellationToken);

        if (!result.IsSuccess || result.Payload is null)
        {
            return AiToolExecution.Failed(result.Message ?? "Boards could not be read.");
        }

        var summaries = result.Payload.Items.Select(board => new
        {
            id = board.Id,
            name = board.Name,
            identifier = board.Identifier,
            boardType = board.BoardType.ToString(),
            projectId = board.ProjectId,
            projectName = board.ProjectName,
            taskCount = board.TaskCount,
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
