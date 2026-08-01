using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.BoardGroups.Queries;

namespace Netptune.Ai.Tools;

public sealed class ListBoardGroupsTool : IAiTool
{
    private readonly IMediator Mediator;

    public ListBoardGroupsTool(IMediator mediator)
    {
        Mediator = mediator;
    }

    public string Name => "list_board_groups";

    public string Description =>
        "List the board groups (board columns) in the current workspace, with the board and project each belongs to.";

    public AiToolKind Kind => AiToolKind.Read;

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.BoardGroups.Read };

    public JsonDocument InputSchema { get; } = AiToolSchema.Empty();

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var options = await Mediator.Send(new GetBoardGroupOptionsQuery(), cancellationToken);
        var summaries = options.Select(option => new
        {
            id = option.Id,
            name = option.Name,
            boardName = option.BoardName,
            boardIdentifier = option.BoardIdentifier,
            projectName = option.ProjectName,
        });

        var content = JsonSerializer.Serialize(summaries);

        return AiToolExecution.Success(content);
    }
}
