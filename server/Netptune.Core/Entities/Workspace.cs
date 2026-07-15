using System.Text.Json.Serialization;

using Netptune.Core.BaseEntities;
using Netptune.Core.Meta;
using Netptune.Core.Relationships;
using Netptune.Core.ViewModels.Workspace;

namespace Netptune.Core.Entities;

public record Workspace : AuditableEntity<int>
{
    public const long DefaultStorageLimitBytes = 5L * 1024 * 1024 * 1024;

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public string Slug { get; set; } = null!;

    public WorkspaceMeta? MetaInfo { get; set; }

    public bool IsPublic { get; set; }

    public long StorageUsedBytes { get; set; }

    public long StorageLimitBytes { get; set; } = DefaultStorageLimitBytes;

    #region NavigationProperties

    [JsonIgnore]
    public ICollection<Project> Projects { get; set; } = new HashSet<Project>();

    [JsonIgnore]
    public ICollection<WorkspaceAppUser> WorkspaceUsers { get; set; } = new HashSet<WorkspaceAppUser>();

    [JsonIgnore]
    public ICollection<ProjectTask> Tasks { get; set; } = new HashSet<ProjectTask>();

    [JsonIgnore]
    public ICollection<WorkspaceFile> Files { get; set; } = new HashSet<WorkspaceFile>();

    [JsonIgnore]
    public ICollection<AppUser> Users { get; set; } = new HashSet<AppUser>();

    #endregion

    #region Methods

    public WorkspaceViewModel ToViewModel()
    {
        return new()
        {
            Id = Id,
            Name = Name,
            Description = Description,
            Slug = Slug,
            MetaInfo = MetaInfo,
            IsPublic = IsPublic,
        };
    }

    #endregion
}
