using System.Text.Json;

using Netptune.Core.Entities;
using Netptune.Core.Enums;
using Netptune.Core.Preferences;
using Netptune.Core.UnitOfWork;

namespace Netptune.Services.Notifications;

public sealed record NotificationRecipientRequest
{
    public IEnumerable<string>? RequestedUserIds { get; init; }

    public required IReadOnlyCollection<string> WorkspaceUserIds { get; init; }

    public required string ActorUserId { get; init; }

    public int WorkspaceId { get; init; }

    public ActivityType ActivityType { get; init; }

    public bool ExcludeActor { get; init; } = true;
}

public static class NotificationRecipientResolver
{
    public static async Task<List<string>> Resolve(
        INetptuneUnitOfWork unitOfWork,
        NotificationRecipientRequest request,
        CancellationToken cancellationToken)
    {
        var candidates = request.RequestedUserIds?
            .Where(userId => request.WorkspaceUserIds.Contains(userId))
            .Where(userId => !request.ExcludeActor || userId != request.ActorUserId)
            .Distinct()
            .ToList() ?? [];

        if (candidates.Count == 0)
        {
            return candidates;
        }

        var key = PreferenceKeys.NotificationEvent(request.ActivityType);
        var values = await unitOfWork.UserPreferences.GetValues(candidates, key, request.WorkspaceId, cancellationToken) ?? [];
        var valuesByUser = values.ToLookup(value => value.UserId);

        return candidates
            .Where(userId => IsEnabled(valuesByUser[userId], request.WorkspaceId))
            .ToList();
    }

    private static bool IsEnabled(IEnumerable<UserPreferenceValue> values, int workspaceId)
    {
        var valuesList = values.ToList();
        var effectiveValue = valuesList.FirstOrDefault(value => value.WorkspaceId == workspaceId)
            ?? valuesList.FirstOrDefault(value => value.WorkspaceId is null);

        return effectiveValue is null || effectiveValue.Value.RootElement.ValueKind is not JsonValueKind.False;
    }
}
