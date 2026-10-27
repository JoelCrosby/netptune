using Netptune.Core.ViewModels.Boards;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Statuses;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Ai.Tools;

// Workspace data a batch of task updates shares, read once for the whole batch rather than once per task.
// A value left null is fetched by whoever needs it.
internal sealed class AiTaskLookups
{
    public Dictionary<int, TaskViewModel>? Tasks { get; init; }

    public List<StatusViewModel>? Statuses { get; set; }

    public Dictionary<string, AssigneeViewModel>? Members { get; init; }

    public HashSet<string>? ExistingTagNames { get; init; }

    public List<BoardGroupOptionViewModel>? BoardGroups { get; init; }

    public Dictionary<int, int?> GroupStatusIds { get; } = [];
}
