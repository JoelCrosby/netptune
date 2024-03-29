﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public record ProjectTaskAppUser : KeyedEntity<int>
{
    public int ProjectTaskId { get; init; }

    public required string UserId { get; init; }

    #region NavigationProperties

    [JsonIgnore]
    public ProjectTask ProjectTask { get; set; } = null!;

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion

    #region Methods

    public static IEnumerable<ProjectTaskAppUser> MergeUsersIds(
        int taskId,
        IEnumerable<ProjectTaskAppUser> current,
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
            yield return new ()
            {
                UserId = selected,
                ProjectTaskId = taskId,
            };
        }
    }

    public override int GetHashCode()
    {
        return (ProjectTaskId, UserId).GetHashCode();
    }

    #endregion
}
