using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.SourceControl;

namespace Netptune.Core.Entities;

public sealed record UserSourceControlIdentity : KeyedEntity<int>
{
    public required string UserId { get; init; }

    public SourceControlProvider Provider { get; init; }

    public string? ExternalId { get; set; }

    public string? Username { get; private set; }

    public string? NormalizedUsername { get; private set; }

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; init; }

    public DateTime UpdatedAt { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion

    public void SetUsername(string? username)
    {
        var trimmed = username?.Trim();
        var hasUsername = !string.IsNullOrEmpty(trimmed);

        Username = hasUsername ? trimmed : null;
        NormalizedUsername = hasUsername ? Normalize(trimmed!) : null;
    }

    public static string Normalize(string username)
    {
        return username.Trim().ToLowerInvariant();
    }
}
