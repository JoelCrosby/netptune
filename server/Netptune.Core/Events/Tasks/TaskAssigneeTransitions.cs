namespace Netptune.Core.Events.Tasks;

// One transition per user, so the activity projection can read each as an assign or unassign rather than an
// opaque "assignees changed". The added user is addressed directly: being assigned is something they need to
// hear about whether or not they subscribe to the task's project, board or sprint.
public static class TaskAssigneeTransitions
{
    public const string Field = "assignees";

    public static List<FieldTransitionedPayload> Split(
        FieldTransitionedPayload template,
        IEnumerable<string> addedUserIds,
        IEnumerable<string> removedUserIds)
    {
        var additions = addedUserIds.Select(userId => template with
        {
            Field = Field,
            OldValue = null,
            NewValue = userId,
            RecipientUserIds = [userId],
        });
        var removals = removedUserIds.Select(userId => template with
        {
            Field = Field,
            OldValue = userId,
            NewValue = null,
            RecipientUserIds = null,
        });
        var transitions = additions.Concat(removals).ToList();

        return transitions;
    }

    public static bool IsAddition(string? field, string? newValue)
    {
        return field == Field && newValue is not null;
    }

    public static bool IsRemoval(string? field, string? oldValue)
    {
        return field == Field && oldValue is not null;
    }
}
