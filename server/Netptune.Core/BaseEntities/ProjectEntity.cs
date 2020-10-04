using System.Text.Json.Serialization;

using Netptune.Core.Entities;

namespace Netptune.Core.BaseEntities
{
    public abstract class ProjectEntity<TEntity> : WorkspaceEntity<TEntity>, IProjectEntity<TEntity>
    {
        public int ProjectId  { get; set; }

        [JsonIgnore]
        public virtual Project Project { get; set; }
    }
}
