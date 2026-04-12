using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public class RefreshToken : IKeyedEntity<int>
{
    public int Id { get; set; }

    public string Token { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public DateTime Expires { get; set; }

    public DateTime Created { get; set; }

    public DateTime? Revoked { get; set; }

    public bool IsExpired => DateTime.UtcNow >= Expires;

    public bool IsRevoked => Revoked is not null;

    public bool IsActive => !IsRevoked && !IsExpired;

    #region NavigationProperties

    [JsonIgnore]
    public AppUser? User { get; set; }

    #endregion
}
