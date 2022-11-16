using System.Text.Json.Serialization;

using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities;

public abstract record WorkspaceEntity<TEntity> : AuditableEntity<TEntity>, IWorkspaceEntity<TEntity>
{
    public int WorkspaceId  { get; set; }

    [JsonIgnore]
    public Workspace Workspace { get; set; } = null!;
}
