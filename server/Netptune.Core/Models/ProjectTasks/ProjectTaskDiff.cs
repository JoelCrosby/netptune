using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Services.Activity;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Models.ProjectTasks;

public record ValueDiff<T>
{
    public bool Modified;

    public T? NewValue;
}

public record ProjectTaskDiff
{
    private ValueDiff<string> Name = null!;

    private ValueDiff<string> Description = null!;

    private ValueDiff<ProjectTaskStatus> Status = null!;

    private ValueDiff<TaskPriority> Priority = null!;

    private ValueDiff<EstimateType> Estimate = null!;

    private AssigneeDiff Assignees = null!;

    private record AssigneeDiff
    {
        public bool Modified;
        public List<string> Added = [];
        public List<string> Removed = [];
    }

    public static ProjectTaskDiff Create(TaskViewModel old, TaskViewModel updated)
    {
        var nameChanged = updated.Name != old.Name;
        var nameValue = updated.Name;

        var descriptionChanged = updated.Description != old.Description;
        var descriptionValue = updated.Description;

        var statusChanged = updated.Status != old.Status;
        var statusValue = updated.Status;

        var priorityChanged = updated.Priority != old.Priority;
        var priorityValue = updated.Priority ?? TaskPriority.None;

        var estimateChanged = updated.EstimateType != old.EstimateType || updated.EstimateValue != old.EstimateValue;
        var estimateTypeValue = updated.EstimateType ?? EstimateType.StoryPoints;

        var oldAssigneeIds = old.Assignees.Select(a => a.Id).ToHashSet();
        var newAssigneeIds = updated.Assignees.Select(a => a.Id).ToHashSet();
        var addedAssignees = newAssigneeIds.Except(oldAssigneeIds).ToList();
        var removedAssignees = oldAssigneeIds.Except(newAssigneeIds).ToList();

        return new ProjectTaskDiff
        {
            Name = new ValueDiff<string>
            {
                Modified = nameChanged,
                NewValue = nameValue,
            },
            Description = new ValueDiff<string>
            {
                Modified = descriptionChanged,
                NewValue = descriptionValue,
            },
            Status = new ValueDiff<ProjectTaskStatus>
            {
                Modified = statusChanged,
                NewValue = statusValue,
            },
            Priority = new ValueDiff<TaskPriority>
            {
                Modified = priorityChanged,
                NewValue = priorityValue,
            },
            Estimate = new ValueDiff<EstimateType>
            {
                Modified = estimateChanged,
                NewValue = estimateTypeValue,
            },
            Assignees = new AssigneeDiff
            {
                Modified = addedAssignees.Count > 0 || removedAssignees.Count > 0,
                Added = addedAssignees,
                Removed = removedAssignees,
            },
        };
    }

    public void LogDiff(IActivityLogger activity, int entityId)
    {
        if (Name.Modified)
        {
            activity.Log(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.ModifyName;
            });
        }

        if (Description.Modified)
        {
            activity.Log(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.ModifyDescription;
            });
        }

        if (Status.Modified)
        {
            activity.Log(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.ModifyStatus;
            });
        }

        if (Priority.Modified)
        {
            activity.Log(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.ModifyPriority;
            });
        }

        if (Estimate.Modified)
        {
            activity.Log(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.ModifyEstimate;
            });
        }

        foreach (var assigneeId in Assignees.Added)
        {
            if (!Assignees.Modified)
            {
                continue;
            }

            activity.LogWith<AssignActivityMeta>(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.Assign;
                options.Meta = new AssignActivityMeta { AssigneeId = assigneeId };
            });
        }

        foreach (var assigneeId in Assignees.Removed)
        {
            if (!Assignees.Modified)
            {
                continue;
            }

            activity.LogWith<AssignActivityMeta>(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.Unassign;
                options.Meta = new AssignActivityMeta { AssigneeId = assigneeId };
            });
        }
    }
}
