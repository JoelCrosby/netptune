using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public class ProjectTaskAppUser : KeyedEntity<int>
{
    public int ProjectTaskId { get; set; }

    public string UserId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ProjectTask ProjectTask { get; set; }

    [JsonIgnore]
    public AppUser User { get; set; }

    #endregion

    #region Methods

    public static IEnumerable<ProjectTaskAppUser> MergeUsersIds(
        int taskId,
        ICollection<ProjectTaskAppUser> current,
        IEnumerable<string> selectedIds)
    {
        var selectedIdSet = selectedIds.ToHashSet();

        foreach (var item in current)
        {
            if (selectedIdSet.Contains(item.UserId))
            {
                selectedIdSet.Remove(item.UserId);

                yield return item;
            }
        }

        foreach (var selected in selectedIdSet)
        {
            yield return new ProjectTaskAppUser
            {
                UserId = selected,
                ProjectTaskId = taskId,
            };
        }
    }

    #endregion
}
