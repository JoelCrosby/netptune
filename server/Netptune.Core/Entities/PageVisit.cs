using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record PageVisit : KeyedEntity<int>
{
    public int PageId { get; set; }

    public string UserId { get; set; } = null!;

    public DateTime VisitedAt { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public Page? Page { get; set; }

    [JsonIgnore]
    public AppUser? User { get; set; }

    #endregion
}
