namespace Netptune.Core.Events.Tasks;

public class RemoveTaskFromBoardActivityMeta
{
    public string Board { get; init; } = null!;

    public int BoardId { get; init; }

    // The group the task sat in on that board, so subscribers to the column hear about the
    // departure and not only subscribers to the board.
    public int? GroupId { get; init; }
}
