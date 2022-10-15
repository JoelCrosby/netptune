namespace Netptune.Core.Events.Tasks;

public class MoveTaskActivityMeta : IActivityMetaData
{
    public string Group { get; init; } = null!;

    public int GroupId { get; init; }
}
