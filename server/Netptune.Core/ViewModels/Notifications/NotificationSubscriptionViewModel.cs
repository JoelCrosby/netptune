using System.Text.Json.Serialization;

using Netptune.Core.Enums;

namespace Netptune.Core.ViewModels.Notifications;

public class NotificationSubscriptionViewModel
{
    public int Id { get; set; }

    public NotificationScope Scope { get; set; }

    public int ScopeEntityId { get; set; }

    public NotificationSubscriptionEvents Events { get; set; }

    public string Name { get; set; } = null!;

    public string? Context { get; set; }

    [JsonIgnore]
    public string? LinkIdentifier { get; set; }

    public string Link { get; set; } = null!;
}

public static class NotificationSubscriptionLink
{
    public static string Build(string workspaceSlug, NotificationScope scope, string? identifier)
    {
        var workspaceRoot = $"/{workspaceSlug}";

        if (string.IsNullOrWhiteSpace(identifier))
        {
            return workspaceRoot;
        }

        return scope switch
        {
            NotificationScope.Project => $"{workspaceRoot}/projects/{identifier}",
            NotificationScope.Board or NotificationScope.BoardGroup => $"{workspaceRoot}/boards/{identifier}",
            NotificationScope.Sprint => $"{workspaceRoot}/sprints/{identifier}",
            _ => workspaceRoot,
        };
    }
}
