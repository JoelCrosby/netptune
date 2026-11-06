namespace Netptune.Core.Models.ProjectTasks;

public sealed record TaskBoardPlacement
{
    public int Id { get; init; }

    public int TaskId { get; init; }

    public int BoardId { get; init; }

    public int BoardGroupId { get; init; }

    public int? GroupStatusId { get; init; }
}
