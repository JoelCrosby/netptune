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

    public ValueDiff<int> Status { get; init; } = null!;

    public ValueDiff<TaskPriority> Priority { get; init; } = null!;

    public ValueDiff<EstimateType> Estimate { get; init; } = null!;

    public ValueDiff<DateOnly?> DueDate { get; init; } = null!;

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
            if (DueDate.Modified) yield return TaskChangeField.DueDate;
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

        var statusChanged = updated.StatusId != old.StatusId;
        var statusValue = updated.StatusId;

        var priorityChanged = updated.Priority != old.Priority;
        var priorityValue = updated.Priority ?? TaskPriority.None;

        var estimateChanged = updated.EstimateType != old.EstimateType || updated.EstimateValue != old.EstimateValue;
        var estimateTypeValue = updated.EstimateType ?? EstimateType.StoryPoints;

        var dueDateChanged = updated.DueDate != old.DueDate;

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
            Status = new ValueDiff<int>
            {
                Modified = statusChanged,
                OldValue = old.StatusId,
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
            DueDate = new ValueDiff<DateOnly?>
            {
                Modified = dueDateChanged,
                OldValue = old.DueDate,
                NewValue = updated.DueDate,
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

        if (DueDate.Modified)
        {
            changes.Add(new TaskFieldChange
            {
                Field = TaskChangeField.DueDate,
                OldValue = DueDate.OldValue?.ToString("yyyy-MM-dd"),
                NewValue = DueDate.NewValue?.ToString("yyyy-MM-dd"),
            });
        }

        if (Assignees.Modified)
        {
            changes.Add(TaskFieldChange.Assignees(Assignees.Added, Assignees.Removed));
        }

        return changes;
    }

    public void LogDiff(IActivityLogger activity, int entityId)
    {
        if (!HasChanges) return;

        activity.LogChanges(options =>
        {
            options.EntityId = entityId;
            options.EntityType = EntityType.Task;

            foreach (var change in ToTaskFieldChanges())
            {
                switch (change.Field)
                {
                    case TaskChangeField.Name:
                        options.Add(ActivityType.ModifyName, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Description:
                        options.Add(ActivityType.ModifyDescription, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Status:
                        options.Add(ActivityType.ModifyStatus, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Priority:
                        options.Add(ActivityType.ModifyPriority, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Estimate:
                        options.Add(ActivityType.ModifyEstimate, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.DueDate:
                        options.Add(ActivityType.ModifyDueDate, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Assignees:
                        foreach (var assigneeId in change.AddedValues)
                        {
                            options.AddWith(ActivityType.Assign, new AssignActivityMeta { AssigneeId = assigneeId });
                        }

                        foreach (var assigneeId in change.RemovedValues)
                        {
                            options.AddWith(ActivityType.Unassign, new AssignActivityMeta { AssigneeId = assigneeId });
                        }

                        break;
                }
            }
        });
    }
}
