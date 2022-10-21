using System.Collections.Generic;
using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Core.Entities;

public record Workspace : AuditableEntity<int>
{
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Slug { get; set; } = null!;

    public WorkspaceMeta? MetaInfo { get; set; }

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<Project> Projects { get; set; } = new HashSet<Project>();

    [JsonIgnore]
    public ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; } = new HashSet<WorkspaceAppUser>();

    [JsonIgnore]
    public ICollection<ProjectTask> Tasks { get; set; } = new HashSet<ProjectTask>();

    [JsonIgnore]
    public ICollection<AppUser> Users { get; set; } = new HashSet<AppUser>();

    #endregion

    #region Methods

    public WorkspaceViewModel ToViewModel()
    {
        return new()
        {
            Name = Name,
            Description = Description,
            Slug = Slug,
            MetaInfo = MetaInfo,
        };
    }

    #endregion
}
