using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
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
        "List the boards in the current workspace, with their id, name, identifier and project.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Boards.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var groups = await Mediator.Send(new GetBoardsInWorkspaceQuery(), cancellationToken);

        if (groups is null)
        {
            return AiToolExecution.Failed("Boards could not be read.");
        }

        var summaries = groups.SelectMany(group => group.Boards.Select(board => new
        {
            id = board.Id,
            name = board.Name,
            identifier = board.Identifier,
            boardType = board.BoardType.ToString(),
            projectId = group.ProjectId,
            projectName = group.ProjectName,
            taskCount = board.TaskCount,
        }));

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
