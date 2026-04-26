using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;

namespace Netptune.Core.Entities;

public record Comment : WorkspaceEntity<int>
{
    public string Body { get; set; } = null!;

    public int EntityId { get; set; }

    public EntityType EntityType { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<Reaction> Reactions { get; set; } = new HashSet<Reaction>();

    [JsonIgnore]
    public ICollection<CommentMention> Mentions { get; set; } = new HashSet<CommentMention>();

    #endregion
}
