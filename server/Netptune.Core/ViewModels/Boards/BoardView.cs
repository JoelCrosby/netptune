using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.Boards;

public class BoardView
{
    public BoardViewModel Board { get; set; } = null!;

    public IEnumerable<BoardViewGroup> Groups { get; set; } = null!;

    public IEnumerable<UserViewModel> Users { get; set; } = null!;
}

public class BoardViewGroup
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int BoardId { get; set; }

    public double SortOrder { get; set; }

    public int? StatusId { get; set; }

    public IList<BoardViewTask> Tasks { get; set; } = null!;
}

public class BoardViewTask
{
    public int Id { get; set; }

    public List<AssigneeViewModel> Assignees { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string SystemId { get; set; } = null!;

    public int StatusId { get; set; }

    public string StatusName { get; set; } = null!;

    public string StatusKey { get; set; } = null!;

    public string? StatusColor { get; set; }

    public StatusCategory StatusCategory { get; set; }

    public IList<string> Tags { get; set; } = null!;

    public TaskPriority? Priority { get; set; }

    public EstimateType? EstimateType { get; set; }

    public decimal? EstimateValue { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public double SortOrder { get; set; }

    public int ProjectId { get; set; }

    public int? SprintId { get; set; }

    public string? SprintName { get; set; }

    public SprintStatus? SprintStatus { get; set; }

    public int WorkspaceId { get; set; }

    public string WorkspaceKey { get; set; } = null!;
}
