using System;
using System.Collections.Generic;

using Netptune.Core.Enums;
using Netptune.Core.Meta;

namespace Netptune.Core.ViewModels.Boards
{
    public class BoardViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Identifier { get; set; }

        public string ProjectName { get; set; }

        public int ProjectId { get; set; }

        public BoardType BoardType { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset? UpdatedAt { get; set; }

        public string OwnerUsername { get; set; }

        public BoardMeta MetaInfo { get; set; }
    }

    public class BoardsViewModel
    {
        public int ProjectId { get; set; }

        public string ProjectName { get; set; }

        public List<BoardViewModel> Boards { get; set; }
    }
}
