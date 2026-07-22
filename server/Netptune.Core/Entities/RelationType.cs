using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Colors;
using Netptune.Core.Enums;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.RelationTypes;

namespace Netptune.Core.Entities;

public record RelationType : WorkspaceEntity<int>
{
    private string? _color;

    public string Name { get; set; } = null!;

    public string InverseName { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string? Description { get; set; }

    public string? Color
    {
        get => _color;
        set => _color = NamedColors.Normalize(value);
    }

    public double SortOrder { get; set; }

    public RelationCategory Category { get; set; }

    public bool IsSystem { get; set; }

    [JsonIgnore]
    public ICollection<ProjectTaskRelation> ProjectTaskRelations { get; } = new HashSet<ProjectTaskRelation>();

    public RelationTypeViewModel ToViewModel()
    {
        return new()
        {
            Id = Id,
            WorkspaceId = WorkspaceId,
            Name = Name,
            InverseName = InverseName,
            Key = Key,
            Description = Description,
            Color = Color,
            SortOrder = SortOrder,
            Category = Category,
            IsSystem = IsSystem,
        };
    }
}
