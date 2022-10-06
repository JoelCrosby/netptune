using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities;

public interface IWorkspaceEntity : IAuditableEntity
{
    public Workspace Workspace  { get; set; }

    public int WorkspaceId  { get; set; }
}

public interface IWorkspaceEntity<TEntity> : IAuditableEntity<TEntity>, IWorkspaceEntity
{
}
