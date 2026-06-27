using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.ProjectTasks;

public sealed class TaskStatusBreakdownViewModel
{
    public int Total { get; set; }

    public List<TaskStatusBreakdownItem> Statuses { get; set; } = [];
}

public sealed class TaskStatusBreakdownItem
{
    public int StatusId { get; set; }

    public string Name { get; set; } = null!;

    public string? Color { get; set; }

    public StatusCategory Category { get; set; }

    public int Count { get; set; }
}
