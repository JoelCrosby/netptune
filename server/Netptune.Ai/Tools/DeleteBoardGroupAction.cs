using System.Text.Json;

using Mediator;

using Netptune.Core.Authorization;
using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Tools;

public sealed class DeleteBoardGroupAction : IAiToolAction
{
    private readonly IMediator Mediator;
    private readonly IAiChangeSetBuilder ChangeSet;

    public DeleteBoardGroupAction(IMediator mediator, IAiChangeSetBuilder changeSet)
    {
        Mediator = mediator;
        ChangeSet = changeSet;
    }

    public string ChangeName => "propose_delete_board_group";

    public IReadOnlySet<string> RequiredPermissions { get; } =
        new HashSet<string>(StringComparer.Ordinal) { NetptunePermissions.BoardGroups.Delete };

    public async Task<AiToolExecution> Execute(JsonElement arguments, CancellationToken cancellationToken)
    {
        var boardGroupId = AiToolSchema.GetInt(arguments, "boardGroupId");

        if (!boardGroupId.HasValue)
        {
            return AiToolExecution.Failed("A boardGroupId is required.");
        }

        var group = await AiBoardLookup.FindGroup(Mediator, boardGroupId.Value, cancellationToken);

        if (group is null)
        {
            return AiToolExecution.Failed($"Board group {boardGroupId} is not in this workspace.");
        }

        var siblings = await AiBoardLookup.FindGroupsInBoard(Mediator, group.BoardId, cancellationToken);
        var fallback = siblings.FirstOrDefault(option => option.Id != group.Id);

        if (fallback is null)
        {
            return AiToolExecution.Failed(
                $"\"{group.Name}\" is the only group on {group.BoardName}, and a board cannot be left without one.");
        }

        var fields = new List<AiChangeField>
        {
            AiChangeFields.Text("boardGroup", group.Name, null),
            AiChangeFields.Text("board", group.BoardName, null),
            AiChangeFields.Text("tasksMoveTo", null, fallback.Name),
        };

        AiToolSchema.AddOptionalField(fields, "reason", AiToolSchema.GetString(arguments, "reason"));

        ChangeSet.Add(new AiChangeDraft
        {
            ToolName = ChangeName,
            EntityType = "boardGroup",
            EntityId = group.Id,
            Summary = $"Delete group “{group.Name}” from {group.BoardName}",
            Fields = fields,
            Payload = JsonSerializer.SerializeToDocument(new { boardGroupId = group.Id }),
            ValidationStatus = AiChangeValidationStatus.Valid,
        });

        return AiToolExecution.Success(
            $"Proposed deleting group \"{group.Name}\", moving any tasks in it to \"{fallback.Name}\". "
            + "Nothing has been deleted yet — the user must review and apply the change.");
    }
}
