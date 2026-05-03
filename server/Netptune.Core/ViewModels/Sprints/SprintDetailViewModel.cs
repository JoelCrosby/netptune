using Netptune.Core.Enums;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.ViewModels.Sprints;

public record SprintDetailViewModel : SprintViewModel
{
    public int NewTaskCount { get; init; }

    public int ActiveTaskCount { get; init; }

    public int DoneTaskCount { get; init; }

    public decimal? TotalEstimateValue { get; init; }

    public EstimateType? EstimateType { get; init; }

    public List<TaskViewModel> Tasks { get; init; } = new();
}
