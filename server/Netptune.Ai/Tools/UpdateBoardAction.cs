using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Encoding;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Handlers.Boards.Queries;

namespace Netptune.Ai.Tools;

public sealed class UpdateBoardAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public UpdateBoardAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_update_board";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.Boards.Update };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var boardId = AiToolSchema.GetInt(arguments, "boardId");

        if (!boardId.HasValue)
        {
            return AiToolExecution.Failed("A boardId is required.");
        }

        var board = await AiBoardLookup.FindBoard(Mediator, boardId.Value, cancellationToken);

        if (board is null)
        {
            return AiToolExecution.Failed($"Board {boardId} is not in this workspace.");
        }

        var name = AiToolSchema.GetString(arguments, "name")?.Trim();
        var identifier = AiToolSchema.GetString(arguments, "identifier")?.Trim().ToUrlSlug();
        var fields = new List<AiChangeField>();
        var payload = new Dictionary<string, object> { ["boardId"] = board.Id };
        var isRenamed = !string.IsNullOrWhiteSpace(name) && !string.Equals(name, board.Name, StringComparison.Ordinal);

        if (isRenamed)
        {
            fields.Add(AiChangeFields.Text("name", board.Name, name));
            payload["name"] = name!;
        }

        var hasIdentifier = !string.IsNullOrWhiteSpace(identifier);
        var isReidentified = hasIdentifier && !string.Equals(identifier, board.Identifier, StringComparison.Ordinal);

        if (isReidentified)
        {
            var uniqueness = await Mediator.Send(new IsBoardIdentifierUniqueQuery(identifier!), cancellationToken);
            var isTaken = uniqueness.Payload?.IsUnique == false;

            if (isTaken)
            {
                return AiToolExecution.Failed($"The board identifier \"{identifier}\" is already in use.");
            }

            fields.Add(AiChangeFields.Text("identifier", board.Identifier, identifier));
            payload["identifier"] = identifier!;
        }

        if (fields.Count == 0)
        {
            return AiToolExecution.Failed("No changes were supplied for this board.");
        }

        var changedNames = string.Join(", ", fields.Select(field => field.Name));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = "board",
            EntityId = board.Id,
            Summary = $"Update {changedNames} on “{board.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(payload),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed updating board {board.Id}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }
}
