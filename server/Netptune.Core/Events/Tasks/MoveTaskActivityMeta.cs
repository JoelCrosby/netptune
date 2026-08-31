namespace Netptune.Core.Events.Tasks;

public class MoveTaskActivityMeta
{
    public string Group { get; init; } = null!;

    public int GroupId { get; init; }

    // Set only when the task joined the board, so a move between groups of a board the task is
    // already on is not mistaken for the task arriving on that board.
    public int? BoardId { get; init; }

    // The group the task was in beforehand. Placement replaces the row rather than editing it, so
    // without this the group a task was dragged out of leaves no trace in the event.
    public int? FromGroupId { get; init; }
}
