using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.ViewModels.Pins;

public sealed record PinnedTaskViewModel
{
    public required TaskViewModel Task { get; init; }

    public required List<TaskPinViewModel> Pins { get; init; }
}
