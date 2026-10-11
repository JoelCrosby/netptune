using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;

namespace Netptune.Core.Entities;

public record Page : WorkspaceEntity<int>
{
    public PageKind Kind { get; set; }

    public int? ParentId { get; set; }

    public int? ProjectId { get; set; }

    public string Title { get; set; } = null!;

    public string Slug { get; set; } = null!;

    public string? Body { get; set; }

    public string? Icon { get; set; }

    public double SortOrder { get; set; }

    public PageStatus Status { get; set; }

    public int Revision { get; set; }

    public DateTime? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public bool IsTemplate { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public Page? Parent { get; set; }

    [JsonIgnore]
    public Project? Project { get; set; }

    [JsonIgnore]
    public AppUser? ReviewedByUser { get; set; }

    [JsonIgnore]
    public ICollection<Page> Children { get; set; } = new HashSet<Page>();

    [JsonIgnore]
    public ICollection<PageTag> PageTags { get; set; } = new HashSet<PageTag>();

    #endregion
}
