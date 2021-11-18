using System.Collections.Generic;

using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.Boards;

public class BoardView
{
    public BoardViewModel Board { get; set; }

    public IEnumerable<BoardViewGroup> Groups { get; set; }

    public IEnumerable<UserViewModel> Users { get; set; }
}

public class BoardViewGroup
{
    public int Id { get; set; }

    public string Name { get; set; }

    public int BoardId { get; set; }

    public BoardGroupType Type { get; set; }

    public double SortOrder { get; set; }

    public IList<BoardViewTask> Tasks { get; set; }
}

public class BoardViewTask
{
    public int Id { get; set; }

    public string AssigneeId { get; set; }

    public string Name { get; set; }

    public string SystemId { get; set; }

    public ProjectTaskStatus Status { get; set; }

    public IList<string> Tags { get; set; }

    public bool IsFlagged { get; set; }

    public double SortOrder { get; set; }

    public int ProjectId { get; set; }

    public int WorkspaceId { get; set; }

    public string WorkspaceKey { get; set; }

    public string AssigneeUsername { get; set; }

    public string AssigneePictureUrl { get; set; }
}