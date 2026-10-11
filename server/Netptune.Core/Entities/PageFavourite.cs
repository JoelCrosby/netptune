using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record PageFavourite : KeyedEntity<int>
{
    public int PageId { get; set; }

    public string UserId { get; set; } = null!;

    public double SortOrder { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public Page? Page { get; set; }

    [JsonIgnore]
    public AppUser? User { get; set; }

    #endregion
}
