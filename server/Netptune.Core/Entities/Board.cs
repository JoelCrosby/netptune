using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.Meta;
using Netptune.Core.ViewModels.Boards;

namespace Netptune.Core.Entities;

public record Board : WorkspaceEntity<int>
{
    public string Name { get; set; } = null!;

    public string Identifier { get; set; } = null!;

    public int ProjectId { get; set; }

    public BoardType BoardType { get; set; }

    public BoardMeta? MetaInfo { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<BoardGroup> BoardGroups { get; set; } = new HashSet<BoardGroup>();

    [JsonIgnore]
    public Project? Project { get; set; }

    #endregion

    public BoardViewModel ToViewModel()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Identifier = Identifier,
            ProjectId = ProjectId,
            BoardType = BoardType,
            CreatedAt = CreatedAt,
            UpdatedAt = UpdatedAt,
            MetaInfo = MetaInfo,
            OwnerUsername = Owner == null ? string.Empty : Owner.DisplayName,
        };
    }
}
