using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public record ProjectTaskTag : KeyedEntity<int>
{
    public int ProjectTaskId { get; set; }

    public int TagId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ProjectTask ProjectTask { get; set; } = null!;

    [JsonIgnore]
    public Tag Tag { get; set; } = null!;

    #endregion

    #region Methods

    public static IEnumerable<ProjectTaskTag> MergeTagIds(
        int taskId,
        IEnumerable<ProjectTaskTag> current,
        IEnumerable<int> selectedIds)
    {
        var selectedIdSet = selectedIds.ToHashSet();

        foreach (var item in current)
        {
            if (selectedIdSet.Contains(item.TagId))
            {
                selectedIdSet.Remove(item.TagId);

                yield return item;
            }
        }

        foreach (var selectedId in selectedIdSet)
        {
            yield return new ProjectTaskTag
            {
                ProjectTaskId = taskId,
                TagId = selectedId,
            };
        }
    }

    #endregion
}
