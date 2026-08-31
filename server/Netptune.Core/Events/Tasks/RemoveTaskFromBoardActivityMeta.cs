namespace Netptune.Core.Events.Tasks;

public class RemoveTaskFromBoardActivityMeta
{
    public string Board { get; init; } = null!;

    public int BoardId { get; init; }
}
