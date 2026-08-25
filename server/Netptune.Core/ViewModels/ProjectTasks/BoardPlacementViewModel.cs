namespace Netptune.Core.ViewModels.ProjectTasks;

public record BoardPlacementViewModel
{
    public int BoardId { get; set; }

    public string BoardName { get; set; } = null!;

    public string BoardIdentifier { get; set; } = null!;

    public int BoardGroupId { get; set; }

    public string BoardGroupName { get; set; } = null!;

    public double SortOrder { get; set; }
}
