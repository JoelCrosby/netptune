using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record PageLink : KeyedEntity<int>
{
    public int PageId { get; set; }

    public PageLinkTargetType TargetType { get; set; }

    public string TargetId { get; set; } = null!;

    public string? Snippet { get; set; }

    public bool IsExplicit { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public Page? Page { get; set; }

    #endregion
}
