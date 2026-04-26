using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record CommentMention : WorkspaceEntity<int>
{
    public int CommentId { get; set; }

    public string UserId { get; set; } = null!;

    #region NavigationProperties

    [JsonIgnore]
    public Comment Comment { get; set; } = null!;

    [JsonIgnore]
    public AppUser User { get; set; } = null!;

    #endregion
}
