using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;

namespace Netptune.Core.Entities;

public record Reaction : WorkspaceEntity<int>
{
    public string Value { get; set; } = null!;

    #region ForeignKeys

    public int CommentId { get; set; }

    #endregion

    #region NavigationProperties

    [JsonIgnore]
    public virtual Comment? Comment { get; set; }

    #endregion
}
