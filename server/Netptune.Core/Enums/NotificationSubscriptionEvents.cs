namespace Netptune.Core.Enums;

[Flags]
public enum NotificationSubscriptionEvents
{
    None = 0,
    TaskCreated = 1,
    TaskUpdated = 2,
    TaskAdded = 4,
    TaskRemoved = 8,

    All = TaskCreated | TaskUpdated | TaskAdded | TaskRemoved,
}
