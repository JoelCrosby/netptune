using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class FlagEntityMap : WorkspaceEntityMap<Flag, int>
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