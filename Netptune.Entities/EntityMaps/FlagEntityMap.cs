using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;
using Netptune.Models;

namespace Netptune.Entities.EntityMaps
{
    public class FlagEntityMap : AuditableEntityMap<Flag, int>
    {
        public override void Configure(EntityTypeBuilder<Flag> builder)
        {
            base.Configure(builder);

            builder
                .Property(flag => flag.Name)
                .HasMaxLength(1024)
                .IsRequired();

            builder
                .Property(flag => flag.Description)
                .HasMaxLength(int.MaxValue);
        }
    }
}
