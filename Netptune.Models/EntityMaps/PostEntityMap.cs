using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{

    public class PostEntityMap : AuditableEntityMap<Post, int>
    {

        public override void Configure(EntityTypeBuilder<Post> builder)
        {
            base.Configure(builder);
        }

    }
}
