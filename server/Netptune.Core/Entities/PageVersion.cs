using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record PageVersion : KeyedEntity<int>
{
    public int PageId { get; set; }

    public int Revision { get; set; }

    public string Title { get; set; } = null!;

    public string? Body { get; set; }

    public string? Summary { get; set; }

    public int? RestoredFromVersionId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    #region NavigationProperties

    [JsonIgnore]
    public Page? Page { get; set; }

    [JsonIgnore]
    public AppUser? CreatedByUser { get; set; }

    #endregion
}
