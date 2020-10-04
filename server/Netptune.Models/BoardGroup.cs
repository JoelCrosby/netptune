using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

using Netptune.Models.BaseEntities;
using Netptune.Models.Enums;
using Netptune.Models.Relationships;
using Netptune.Models.ViewModels.ProjectTasks;

namespace Netptune.Models
{
    public class BoardGroup : AuditableEntity<int>
    {
        public string Name { get; set; }

        public int BoardId { get; set; }

        public BoardGroupType Type { get; set; }

        public double SortOrder { get; set; }

        #region NavigationProperties

        [JsonIgnore]
        public Board Board { get; set; }

        [JsonIgnore]
        public ICollection<ProjectTaskInBoardGroup> TasksInGroups { get; set; } = new HashSet<ProjectTaskInBoardGroup>();

        #endregion

        #region NotMapped

        [NotMapped]
        public List<TaskViewModel> Tasks { get; set; } = new List<TaskViewModel>();

        #endregion
    }
}