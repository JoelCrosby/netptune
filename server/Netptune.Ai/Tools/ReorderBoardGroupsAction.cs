using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Ai.Tools;

public sealed class ReorderBoardGroupsAction : IAiToolAction
{
    private const string OrderSeparator = " → ";

    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public ReorderBoardGroupsAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_reorder_board_groups";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.BoardGroups.Update };

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

        var groups = await AiBoardLookup.FindGroupsInBoard(Mediator, board.Id, cancellationToken);

        if (groups.Count == 0)
        {
            return AiToolExecution.Failed($"{board.Name} has no groups to order.");
        }

        var groupIds = AiToolSchema.GetIntArray(arguments, "groupIds");
        var groupsById = groups.ToDictionary(group => group.Id);
        var unknown = groupIds.Where(id => !groupsById.ContainsKey(id)).ToList();

        if (unknown.Count > 0)
        {
            return AiToolExecution.Failed(
                $"Group {string.Join(", ", unknown)} is not on {board.Name}. Order the groups of one board at a time.");
        }

        var coversEveryGroup = groupIds.Distinct().Count() == groupIds.Count && groupIds.Count == groups.Count;

        if (!coversEveryGroup)
        {
            return AiToolExecution.Failed(
                $"groupIds must list each of the {groups.Count} groups on {board.Name} exactly once.");
        }

        var isUnchanged = groups.Select(group => group.Id).SequenceEqual(groupIds);

        if (isUnchanged)
        {
            return AiToolExecution.Failed($"The groups on {board.Name} are already in that order.");
        }

        var ordered = groupIds.Select(id => groupsById[id]).ToList();
        var fields = new List<AiChangeField>
        {
            AiChangeFields.Text("order", Describe(groups), Describe(ordered)),
        };

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = "board",
            EntityId = board.Id,
            Summary = $"Reorder the groups on “{board.Name}”",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { boardId = board.Id, groupIds }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed ordering the groups on {board.Name} as {Describe(ordered)}. "
            + "Nothing has been applied yet — the user must review and apply the change.");
    }

    private static string Describe(List<BoardGroupOptionViewModel> groups)
    {
        return string.Join(OrderSeparator, groups.Select(group => group.Name));
    }
}
