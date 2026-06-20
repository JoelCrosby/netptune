using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Enums;
using Netptune.Core.ViewModels.Statuses;

namespace Netptune.Core.Entities;

public record Status : WorkspaceEntity<int>
{
    public EntityType EntityType { get; set; }

    public string Name { get; set; } = null!;

    public string Key { get; set; } = null!;

    public string? Description { get; set; }

    public string? Color { get; set; }

    public double SortOrder { get; set; }

    public StatusCategory Category { get; set; }

    public bool IsSystem { get; set; }

    [JsonIgnore]
    public ICollection<ProjectTask> ProjectTasks { get; } = new HashSet<ProjectTask>();

    public StatusViewModel ToViewModel()
    {
        return new()
        {
            Id = Id,
            WorkspaceId = WorkspaceId,
            EntityType = EntityType,
            Name = Name,
            Key = Key,
            Description = Description,
            Color = Color,
            SortOrder = SortOrder,
            Category = Category,
            IsSystem = IsSystem,
        };
    }

}
