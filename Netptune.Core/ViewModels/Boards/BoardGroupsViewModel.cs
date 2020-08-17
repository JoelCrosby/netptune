using System.Collections.Generic;

using Netptune.Core.Entities;
using Netptune.Core.ViewModels.Users;

namespace Netptune.Core.ViewModels.Boards
{
    public class BoardGroupsViewModel
    {
        public BoardViewModel Board { get; set; }

        public List<BoardGroup> Groups { get; set; }

        public List<UserViewModel> Users { get; set; }
    }
}
