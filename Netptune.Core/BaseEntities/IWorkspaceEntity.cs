namespace Netptune.Core.BaseEntities
{
    public interface IWorkspaceEntity<TEntity> : IAuditableEntity<TEntity>
    {
        public int WorkspaceId  { get; set; }
    }
}
