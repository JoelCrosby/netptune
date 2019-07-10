using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class FlagEntityMap : AuditableEntityMap<Flag, int>
    {
        public override void Configure(EntityTypeBuilder<Flag> builder)
        {
            base.Configure(builder);
        }
    }
}
