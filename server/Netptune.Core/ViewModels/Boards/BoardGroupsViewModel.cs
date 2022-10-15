using System.Collections.Generic;

using Netptune.Core.Entities;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.Boards;

public class BoardGroupsViewModel
{
    public BoardViewModel Board { get; set; } = null!;

    public IEnumerable<BoardGroup> Groups { get; set; } = null!;

    public IEnumerable<UserViewModel> Users { get; set; } = null!;
}
