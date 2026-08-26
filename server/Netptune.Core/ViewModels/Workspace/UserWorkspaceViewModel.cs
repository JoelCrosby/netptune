using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.Workspace;

public class UserWorkspaceViewModel : WorkspaceViewModel
{
    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool IsLastVisited { get; set; }

    public List<AssigneeViewModel> Members { get; set; } = [];

    public int MemberCount { get; set; }
}
