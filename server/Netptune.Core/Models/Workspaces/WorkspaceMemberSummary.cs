using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.Models.Workspaces;

public sealed record WorkspaceMemberSummary
{
    public int MemberCount { get; init; }

    public List<AssigneeViewModel> Members { get; init; } = [];
}
