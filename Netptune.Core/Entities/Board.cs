using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Entities
{
    public class Board : AuditableEntity<int>
    {
        public string Name { get; set; }

        public string Identifier { get; set; }

        public int ProjectId { get; set; }

        public BoardType BoardType { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<BoardGroup> BoardGroups { get; set; } = new HashSet<BoardGroup>();

        [JsonIgnore]
        public Project Project { get; set; }

        #endregion

        public BoardViewModel ToViewModel()
        {
            return new BoardViewModel
            {
                Id = Id,
                Name = Name,
                Identifier = Identifier,
                ProjectId = ProjectId,
                Project = Project,
                BoardType = BoardType,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt,
                OwnerUsername = Owner == null ? string.Empty : Owner.GetDisplayName(),
            };
        }

    }
}
