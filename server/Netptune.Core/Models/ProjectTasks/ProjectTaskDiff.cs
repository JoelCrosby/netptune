using Netptune.Core.Enums;
using Netptune.Core.Events.Tasks;
using Netptune.Core.Services.Activity;
using Netptune.Core.ViewModels.ProjectTasks;

namespace Netptune.Core.Models.ProjectTasks;

public record ValueDiff<T>
{
    public bool Modified { get; init; }

    public T? OldValue { get; init; }

    public T? NewValue { get; init; }
}

public record ProjectTaskDiff
{
    public ValueDiff<string> Name { get; init; } = null!;

    public ValueDiff<string> Description { get; init; } = null!;

    public ValueDiff<ProjectTaskStatus> Status { get; init; } = null!;

    public ValueDiff<TaskPriority> Priority { get; init; } = null!;

    public ValueDiff<EstimateType> Estimate { get; init; } = null!;

    public AssigneeDiff Assignees { get; init; } = null!;

    public bool HasChanges => ChangedFields.Any();

    public IEnumerable<TaskChangeField> ChangedFields
    {
        get
        {
            if (Name.Modified) yield return TaskChangeField.Name;
            if (Description.Modified) yield return TaskChangeField.Description;
            if (Status.Modified) yield return TaskChangeField.Status;
            if (Priority.Modified) yield return TaskChangeField.Priority;
            if (Estimate.Modified) yield return TaskChangeField.Estimate;
            if (Assignees.Modified) yield return TaskChangeField.Assignees;
        }
    }

    public record AssigneeDiff
    {
        public bool Modified { get; init; }

        public List<string> Added { get; init; } = [];

        public List<string> Removed { get; init; } = [];
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
                OldValue = old.Name,
                NewValue = nameValue,
            },
            Description = new ValueDiff<string>
            {
                Modified = descriptionChanged,
                OldValue = old.Description,
                NewValue = descriptionValue,
            },
            Status = new ValueDiff<ProjectTaskStatus>
            {
                Modified = statusChanged,
                OldValue = old.Status,
                NewValue = statusValue,
            },
            Priority = new ValueDiff<TaskPriority>
            {
                Modified = priorityChanged,
                OldValue = old.Priority ?? TaskPriority.None,
                NewValue = priorityValue,
            },
            Estimate = new ValueDiff<EstimateType>
            {
                Modified = estimateChanged,
                OldValue = old.EstimateType ?? EstimateType.StoryPoints,
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

    public List<TaskFieldChange> ToTaskFieldChanges()
    {
        var changes = new List<TaskFieldChange>();

        if (Name.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Name, Name.OldValue, Name.NewValue));
        }

        if (Description.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Description, Description.OldValue, Description.NewValue));
        }

        if (Status.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Status, Status.OldValue, Status.NewValue));
        }

        if (Priority.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Priority, Priority.OldValue, Priority.NewValue));
        }

        if (Estimate.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Estimate, Estimate.OldValue, Estimate.NewValue));
        }

        if (Assignees.Modified)
        {
            changes.Add(TaskFieldChange.Assignees(Assignees.Added, Assignees.Removed));
        }

        return changes;
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
