using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Comments;

namespace Netptune.Core.Entities;

public record Comment : WorkspaceEntity<int>
{
    public string Body { get; set; } = null!;

    public int EntityId { get; set; }

    public EntityType EntityType { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<Reaction> Reactions { get; set; } = new HashSet<Reaction>();

    #endregion

    public CommentViewModel ToViewModel()
    {
        return new()
        {
            Id = Id,
            UserDisplayName = Owner!.DisplayName,
            UserDisplayImage= Owner.PictureUrl,
            UserId = OwnerId!,
            Body = Body,
            EntityId = EntityId,
            EntityType = EntityType,
            Reactions = Reactions.ToList(),
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
        };
    }
}
