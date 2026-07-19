using Netptune.Core.Enums;

namespace Netptune.Core.Events.Tasks;

public record TaskChangedMessage : IEventMessage
{
    public static string Subject => MessageKeys.Subjects.Automation;

    public Guid EventId { get; init; } = Guid.NewGuid();

    public int WorkspaceId { get; init; }

    public int TaskId { get; init; }

    public string ActorUserId { get; init; } = null!;

    public List<TaskFieldChange> Changes { get; init; } = [];

    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}

public record TaskFieldChange
{
    public TaskChangeField Field { get; init; }

    public string? OldValue { get; init; }

    public string? NewValue { get; init; }

    public List<string> AddedValues { get; init; } = [];

    public List<string> RemovedValues { get; init; } = [];

    public static TaskFieldChange Create(TaskChangeField field, object? oldValue, object? newValue)
    {
        return new TaskFieldChange
        {
            Field = field,
            OldValue = oldValue?.ToString(),
            NewValue = newValue?.ToString(),
        };
    }

    public static TaskFieldChange Assignees(IEnumerable<string> addedValues, IEnumerable<string> removedValues)
    {
        return new TaskFieldChange
        {
            Field = TaskChangeField.Assignees,
            AddedValues = addedValues.ToList(),
            RemovedValues = removedValues.ToList(),
        };
    }

    public static TaskFieldChange Tags(IEnumerable<string> addedValues, IEnumerable<string> removedValues)
    {
        return new TaskFieldChange
        {
            Field = TaskChangeField.Tags,
            AddedValues = addedValues.ToList(),
            RemovedValues = removedValues.ToList(),
        };
    }
}
