using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Tags;

namespace Netptune.Core.Entities
{
    public class Tag : WorkspaceEntity<int>
    {
        public string Name { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public ICollection<ProjectTaskTag> ProjectTaskTags { get; set; } = new HashSet<ProjectTaskTag>();

        [JsonIgnore]
        public ICollection<ProjectTask> Tasks { get; set; } = new HashSet<ProjectTask>();

        #endregion

        public TagViewModel ToViewModel()
        {
            return new TagViewModel
            {
                Id = Id,
                Name = Name,
                OwnerId = OwnerId,
                OwnerName = Owner?.DisplayName,
            };
        }
    }
}
