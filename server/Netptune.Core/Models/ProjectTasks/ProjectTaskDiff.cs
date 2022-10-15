using Netptune.Core.Enums;
using Netptune.Core.Events;
using Netptune.Core.Events.Tasks;
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

    private ValueDiff<bool> Flagged = null!;

    private ValueDiff<ProjectTaskStatus> Status = null!;

    private ValueDiff<string> Assignee = null!;

    public static ProjectTaskDiff Create(TaskViewModel old, TaskViewModel updated)
    {
        var nameChanged = updated.Name != old.Name;
        var nameValue = updated.Name;

        var descriptionChanged = updated.Description != old.Description;
        var descriptionValue = updated.Description;

        var flaggedChanged = updated.IsFlagged != old.IsFlagged;
        var flaggedValue = updated.IsFlagged;

        var statusChanged = updated.Status != old.Status;
        var statusValue = updated.Status;

        // TODO: Implement change detection for multiple assignees

        var assigneeChanged = false; // updated.AssigneeId != old.AssigneeId && !string.IsNullOrEmpty(old.AssigneeId);
        var assigneeValue = string.Empty; // updated.AssigneeId;

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
            Flagged = new ValueDiff<bool>
            {
                Modified = flaggedChanged,
                NewValue = flaggedValue,
            },
            Status = new ValueDiff<ProjectTaskStatus>
            {
                Modified = statusChanged,
                NewValue = statusValue,
            },
            Assignee = new ValueDiff<string>
            {
                Modified = assigneeChanged,
                NewValue = assigneeValue,
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

        if (Flagged.Modified)
        {
            activity.Log(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type =  Flagged.NewValue ? ActivityType.Flag : ActivityType.UnFlag;
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

        if (Assignee.Modified)
        {
            activity.LogWith<AssignActivityMeta>(options =>
            {
                options.EntityId = entityId;
                options.EntityType = EntityType.Task;
                options.Type = ActivityType.Assign;
                options.Meta = new AssignActivityMeta
                {
                    AssigneeId = Assignee.NewValue,
                };
            });
        }
    }
}
