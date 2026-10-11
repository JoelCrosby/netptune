using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Entities;

namespace Netptune.Core.Relationships;

public record PageTag : KeyedEntity<int>
{
    public int PageId { get; set; }

    public int TagId { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public Page? Page { get; set; }

    [JsonIgnore]
    public Tag? Tag { get; set; }

    #endregion
}
