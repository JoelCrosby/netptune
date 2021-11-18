using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities;

public interface IWorkspaceEntity<TEntity> : IAuditableEntity<TEntity>
{
    public Workspace Workspace  { get; set; }

    public int WorkspaceId  { get; set; }
}