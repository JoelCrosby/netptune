using System.Collections.Generic;

using Netptune.Core.Entities;

namespace Netptune.Core.ViewModels.Boards
{
    public class BoardGroupsViewModel
    {
        public BoardViewModel Board { get; set; }

        public List<BoardGroup> Groups { get; set; }
    }
}
