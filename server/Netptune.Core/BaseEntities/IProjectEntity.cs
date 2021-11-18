using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities;

public interface IProjectEntity<TEntity> : IWorkspaceEntity<TEntity>
{
    public Project Project { get; set; }

    public int ProjectId  { get; set; }
}