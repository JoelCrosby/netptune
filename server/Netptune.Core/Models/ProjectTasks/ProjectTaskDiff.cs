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

    public ValueDiff<string> Owner { get; init; } = null!;

    public ValueDiff<TaskPriority> Priority { get; init; } = null!;

    public ValueDiff<EstimateType> Estimate { get; init; } = null!;

    public ValueDiff<DateOnly?> StartDate { get; init; } = null!;

    public ValueDiff<DateOnly?> DueDate { get; init; } = null!;

    public ValueDiff<int?> Sprint { get; init; } = null!;

    public ValueDiff<int?> BoardGroup { get; init; } = null!;

    public AssigneeDiff Assignees { get; init; } = null!;

    public TagDiff Tags { get; init; } = null!;

    public bool HasChanges => ChangedFields.Any();

    public IEnumerable<TaskChangeField> ChangedFields
    {
        get
        {
            if (Name.Modified) yield return TaskChangeField.Name;
            if (Description.Modified) yield return TaskChangeField.Description;
            if (Status.Modified) yield return TaskChangeField.Status;
            if (Owner.Modified) yield return TaskChangeField.Owner;
            if (Priority.Modified) yield return TaskChangeField.Priority;
            if (Estimate.Modified) yield return TaskChangeField.Estimate;

            if (StartDate.Modified)
            {
                yield return TaskChangeField.StartDate;
            }

            if (DueDate.Modified) yield return TaskChangeField.DueDate;
            if (Sprint.Modified) yield return TaskChangeField.Sprint;
            if (BoardGroup.Modified) yield return TaskChangeField.BoardGroup;
            if (Assignees.Modified) yield return TaskChangeField.Assignees;
            if (Tags.Modified) yield return TaskChangeField.Tags;
        }
    }

    public record AssigneeDiff
    {
        public bool Modified { get; init; }

        public List<string> Added { get; init; } = [];

        public List<string> Removed { get; init; } = [];
    }

    public record TagDiff
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

        var ownerChanged = updated.OwnerId != old.OwnerId;

        var priorityChanged = updated.Priority != old.Priority;
        var priorityValue = updated.Priority ?? TaskPriority.None;

        var estimateChanged = updated.EstimateType != old.EstimateType || updated.EstimateValue != old.EstimateValue;
        var estimateTypeValue = updated.EstimateType ?? EstimateType.StoryPoints;

        var startDateChanged = updated.StartDate != old.StartDate;
        var dueDateChanged = updated.DueDate != old.DueDate;
        var sprintChanged = updated.SprintId != old.SprintId;
        var boardGroupChanged = updated.BoardGroupId != old.BoardGroupId;

        var oldAssigneeIds = old.Assignees.Select(a => a.Id).ToHashSet();
        var newAssigneeIds = updated.Assignees.Select(a => a.Id).ToHashSet();
        var addedAssignees = newAssigneeIds.Except(oldAssigneeIds).ToList();
        var removedAssignees = oldAssigneeIds.Except(newAssigneeIds).ToList();

        var oldTags = old.Tags.ToHashSet(StringComparer.Ordinal);
        var newTags = updated.Tags.ToHashSet(StringComparer.Ordinal);
        var addedTags = newTags.Except(oldTags).ToList();
        var removedTags = oldTags.Except(newTags).ToList();

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
            Owner = new ValueDiff<string>
            {
                Modified = ownerChanged,
                OldValue = old.OwnerId,
                NewValue = updated.OwnerId,
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
            StartDate = new ValueDiff<DateOnly?>
            {
                Modified = startDateChanged,
                OldValue = old.StartDate,
                NewValue = updated.StartDate,
            },
            DueDate = new ValueDiff<DateOnly?>
            {
                Modified = dueDateChanged,
                OldValue = old.DueDate,
                NewValue = updated.DueDate,
            },
            Sprint = new ValueDiff<int?>
            {
                Modified = sprintChanged,
                OldValue = old.SprintId,
                NewValue = updated.SprintId,
            },
            BoardGroup = new ValueDiff<int?>
            {
                Modified = boardGroupChanged,
                OldValue = old.BoardGroupId,
                NewValue = updated.BoardGroupId,
            },
            Assignees = new AssigneeDiff
            {
                Modified = addedAssignees.Count > 0 || removedAssignees.Count > 0,
                Added = addedAssignees,
                Removed = removedAssignees,
            },
            Tags = new TagDiff
            {
                Modified = addedTags.Count > 0 || removedTags.Count > 0,
                Added = addedTags,
                Removed = removedTags,
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

        if (Owner.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Owner, Owner.OldValue, Owner.NewValue));
        }

        if (Priority.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Priority, Priority.OldValue, Priority.NewValue));
        }

        if (Estimate.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Estimate, Estimate.OldValue, Estimate.NewValue));
        }

        if (StartDate.Modified)
        {
            changes.Add(new TaskFieldChange
            {
                Field = TaskChangeField.StartDate,
                OldValue = StartDate.OldValue?.ToString("yyyy-MM-dd"),
                NewValue = StartDate.NewValue?.ToString("yyyy-MM-dd"),
            });
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

        if (Sprint.Modified)
        {
            changes.Add(TaskFieldChange.Create(TaskChangeField.Sprint, Sprint.OldValue, Sprint.NewValue));
        }

        if (BoardGroup.Modified)
        {
            changes.Add(TaskFieldChange.Create(
                TaskChangeField.BoardGroup,
                BoardGroup.OldValue,
                BoardGroup.NewValue));
        }

        if (Assignees.Modified)
        {
            changes.Add(TaskFieldChange.Assignees(Assignees.Added, Assignees.Removed));
        }

        if (Tags.Modified)
        {
            changes.Add(TaskFieldChange.Tags(Tags.Added, Tags.Removed));
        }

        return changes;
    }

    public void LogDiff(
        IActivityLogger activity,
        int entityId,
        int? workspaceId = null,
        string? actorUserId = null)
    {
        if (!HasChanges)
        {
            return;
        }

        activity.LogChanges(options =>
        {
            options.EntityId = entityId;
            options.EntityType = EntityType.Task;
            options.WorkspaceId = workspaceId;
            options.UserId = actorUserId ?? options.UserId;

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

                    case TaskChangeField.Owner:
                        options.Add(ActivityType.Modify, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Priority:
                        options.Add(ActivityType.ModifyPriority, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Estimate:
                        options.Add(ActivityType.ModifyEstimate, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.StartDate:
                        options.Add(ActivityType.ModifyStartDate, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.DueDate:
                        options.Add(ActivityType.ModifyDueDate, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.Sprint:
                        options.Add(ActivityType.Move, change.Field, change.OldValue, change.NewValue);
                        break;

                    case TaskChangeField.BoardGroup:
                        options.Add(ActivityType.Move, change.Field, change.OldValue, change.NewValue);
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

                    case TaskChangeField.Tags:
                        foreach (var tag in change.AddedValues)
                        {
                            options.Add(ActivityType.AddTag, change.Field, null, tag);
                        }

                        foreach (var tag in change.RemovedValues)
                        {
                            options.Add(ActivityType.RemoveTag, change.Field, tag, null);
                        }

                        break;
                }
            }
        });
    }
}
