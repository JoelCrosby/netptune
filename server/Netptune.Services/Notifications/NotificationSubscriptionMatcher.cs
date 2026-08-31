using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Models.Activity;
using Netptune.Core.Repositories;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Notifications;

public sealed record NotificationSubscriptionMatchRequest
{
    public required int WorkspaceId { get; init; }

    public required EntityType EntityType { get; init; }

    public required int EntityId { get; init; }

    public required ActivityType ActivityType { get; init; }

    // The event record payload, when one is available. Merged activity entries carry no per-event
    // payload, and without it a task event can only be read as a change within its current scopes.
    public JsonElement? Payload { get; init; }
}

// Resolves which people a batch of task events reaches through their per-scope subscriptions. Built
// once per batch rather than per event: a bulk move logs one event per task, and a lookup per event
// would put two more queries inside the notification transaction for every card moved.
public sealed class NotificationSubscriptionFanOut
{
    private static readonly IReadOnlyList<string> NoRecipients = [];

    private readonly IReadOnlyDictionary<int, TaskScopes> ScopesByTaskId;
    private readonly ILookup<int, NotificationSubscription> SubscriptionsByWorkspace;

    private NotificationSubscriptionFanOut(
        IReadOnlyDictionary<int, TaskScopes> scopesByTaskId,
        ILookup<int, NotificationSubscription> subscriptionsByWorkspace)
    {
        ScopesByTaskId = scopesByTaskId;
        SubscriptionsByWorkspace = subscriptionsByWorkspace;
    }

    public static async Task<NotificationSubscriptionFanOut> Build(
        INetptuneUnitOfWork unitOfWork,
        IReadOnlyCollection<NotificationSubscriptionMatchRequest> requests,
        CancellationToken cancellationToken)
    {
        var taskRequests = requests.Where(request => request.EntityType is EntityType.Task).ToList();

        if (taskRequests.Count == 0)
        {
            return Empty();
        }

        var taskIds = taskRequests.Select(request => request.EntityId).Distinct().ToList();
        var scopesByTaskId = await unitOfWork.Ancestors.GetTaskScopes(taskIds, cancellationToken);
        var subscriptions = new List<NotificationSubscription>();

        foreach (var workspace in taskRequests.GroupBy(request => request.WorkspaceId))
        {
            var keys = workspace
                .SelectMany(request => ScopeEventsFor(request, scopesByTaskId).Keys)
                .Distinct()
                .ToList();

            if (keys.Count == 0)
            {
                continue;
            }

            var query = BuildQuery(workspace.Key, keys);
            var matches = await unitOfWork.NotificationSubscriptions.GetForScopes(query, cancellationToken);

            subscriptions.AddRange(matches);
        }

        return new NotificationSubscriptionFanOut(
            scopesByTaskId,
            subscriptions.ToLookup(subscription => subscription.WorkspaceId));
    }

    public IReadOnlyList<string> Recipients(NotificationSubscriptionMatchRequest request)
    {
        if (request.EntityType is not EntityType.Task)
        {
            return NoRecipients;
        }

        var scopeEvents = ScopeEventsFor(request, ScopesByTaskId);

        if (scopeEvents.Count == 0)
        {
            return NoRecipients;
        }

        return SubscriptionsByWorkspace[request.WorkspaceId]
            .Where(subscription => Matches(subscription, scopeEvents))
            .Select(subscription => subscription.UserId)
            .Distinct()
            .ToList();
    }

    private static NotificationSubscriptionFanOut Empty()
    {
        var subscriptions = Array.Empty<NotificationSubscription>();

        return new NotificationSubscriptionFanOut(
            new Dictionary<int, TaskScopes>(),
            subscriptions.ToLookup(subscription => subscription.WorkspaceId));
    }

    private static Dictionary<NotificationScopeKey, NotificationSubscriptionEvents> ScopeEventsFor(
        NotificationSubscriptionMatchRequest request,
        IReadOnlyDictionary<int, TaskScopes> scopesByTaskId)
    {
        var hasScopes = scopesByTaskId.TryGetValue(request.EntityId, out var scopes);

        if (!hasScopes)
        {
            return [];
        }

        return NotificationScopeClassifier.Classify(request.ActivityType, scopes!, request.Payload);
    }

    private static bool Matches(
        NotificationSubscription subscription,
        IReadOnlyDictionary<NotificationScopeKey, NotificationSubscriptionEvents> scopeEvents)
    {
        var key = new NotificationScopeKey(subscription.Scope, subscription.ScopeEntityId);
        var coversScope = scopeEvents.TryGetValue(key, out var events);
        var coversEvent = coversScope && (subscription.Events & events) != NotificationSubscriptionEvents.None;

        return coversEvent;
    }

    private static NotificationSubscriptionScopeQuery BuildQuery(
        int workspaceId,
        IEnumerable<NotificationScopeKey> keys)
    {
        var byScope = keys.ToLookup(key => key.Scope, key => key.EntityId);

        return new NotificationSubscriptionScopeQuery
        {
            WorkspaceId = workspaceId,
            ProjectIds = byScope[NotificationScope.Project].ToList(),
            BoardIds = byScope[NotificationScope.Board].ToList(),
            BoardGroupIds = byScope[NotificationScope.BoardGroup].ToList(),
            SprintIds = byScope[NotificationScope.Sprint].ToList(),
        };
    }
}

public readonly record struct NotificationScopeKey(NotificationScope Scope, int EntityId);

public static class NotificationScopeClassifier
{
    private sealed record ScopeTransitions(List<NotificationScopeKey> Entered, List<NotificationScopeKey> Exited);

    // What one task event means for each scope the task touches. A scope the task entered reads as
    // an addition and one it left as a removal; everything else about the same event is a change
    // inside the scopes the task already sits in.
    public static Dictionary<NotificationScopeKey, NotificationSubscriptionEvents> Classify(
        ActivityType activityType,
        TaskScopes scopes,
        JsonElement? payload)
    {
        var baseline = Baseline(activityType);
        var scopeEvents = new Dictionary<NotificationScopeKey, NotificationSubscriptionEvents>();

        foreach (var key in CurrentKeys(scopes))
        {
            scopeEvents[key] = baseline;
        }

        var transitions = ReadTransitions(activityType, payload);

        foreach (var key in transitions.Exited)
        {
            scopeEvents[key] = NotificationSubscriptionEvents.TaskRemoved;
        }

        foreach (var key in transitions.Entered)
        {
            scopeEvents[key] = NotificationSubscriptionEvents.TaskAdded;
        }

        return scopeEvents;
    }

    private static NotificationSubscriptionEvents Baseline(ActivityType activityType) => activityType switch
    {
        ActivityType.Create => NotificationSubscriptionEvents.TaskCreated,
        ActivityType.Delete => NotificationSubscriptionEvents.TaskRemoved,
        _ => NotificationSubscriptionEvents.TaskUpdated,
    };

    private static IEnumerable<NotificationScopeKey> CurrentKeys(TaskScopes scopes)
    {
        if (scopes.ProjectId.HasValue)
        {
            yield return new NotificationScopeKey(NotificationScope.Project, scopes.ProjectId.Value);
        }

        if (scopes.SprintId.HasValue)
        {
            yield return new NotificationScopeKey(NotificationScope.Sprint, scopes.SprintId.Value);
        }

        foreach (var boardId in scopes.BoardIds)
        {
            yield return new NotificationScopeKey(NotificationScope.Board, boardId);
        }

        foreach (var boardGroupId in scopes.BoardGroupIds)
        {
            yield return new NotificationScopeKey(NotificationScope.BoardGroup, boardGroupId);
        }
    }

    private static ScopeTransitions ReadTransitions(ActivityType activityType, JsonElement? payload)
    {
        var entered = new List<NotificationScopeKey>();
        var exited = new List<NotificationScopeKey>();
        var carriesTransition = payload.HasValue && activityType is ActivityType.Move or ActivityType.Remove;

        if (!carriesTransition)
        {
            return new ScopeTransitions(entered, exited);
        }

        var element = payload!.Value;
        var boardId = ReadNumber(element, "boardId");

        if (activityType is ActivityType.Remove)
        {
            AddKey(exited, NotificationScope.Board, boardId);
            AddKey(exited, NotificationScope.BoardGroup, ReadNumber(element, "groupId"));

            return new ScopeTransitions(entered, exited);
        }

        AddKey(entered, NotificationScope.Board, boardId);
        AddKey(entered, NotificationScope.BoardGroup, ReadNumber(element, "groupId"));
        AddKey(exited, NotificationScope.BoardGroup, ReadNumber(element, "fromGroupId"));

        var movedScope = ScopeForField(ReadText(element, "field"));

        if (movedScope.HasValue)
        {
            AddKey(exited, movedScope.Value, ReadNumberText(element, "oldValue"));
            AddKey(entered, movedScope.Value, ReadNumberText(element, "newValue"));
        }

        return new ScopeTransitions(entered, exited);
    }

    private static void AddKey(List<NotificationScopeKey> keys, NotificationScope scope, int? entityId)
    {
        if (!entityId.HasValue)
        {
            return;
        }

        keys.Add(new NotificationScopeKey(scope, entityId.Value));
    }

    private static NotificationScope? ScopeForField(string? field) => field switch
    {
        nameof(TaskChangeField.Sprint) => NotificationScope.Sprint,
        nameof(TaskChangeField.BoardGroup) => NotificationScope.BoardGroup,
        _ => null,
    };

    private static int? ReadNumber(JsonElement element, string propertyName)
    {
        var exists = element.TryGetProperty(propertyName, out var value);
        var isNumber = exists && value.ValueKind is JsonValueKind.Number;

        if (!isNumber)
        {
            return null;
        }

        return value.TryGetInt32(out var number) ? number : null;
    }

    // Field transitions travel through the ledger as the text form of the value they changed, so a
    // sprint or board group id arrives as a JSON string rather than a number.
    private static int? ReadNumberText(JsonElement element, string propertyName)
    {
        var text = ReadText(element, propertyName);

        if (text is null)
        {
            return null;
        }

        return int.TryParse(text, out var number) ? number : null;
    }

    private static string? ReadText(JsonElement element, string propertyName)
    {
        var exists = element.TryGetProperty(propertyName, out var value);
        var isText = exists && value.ValueKind is JsonValueKind.String;

        return isText ? value.GetString() : null;
    }
}
