using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class WorkspaceAppUserEntityMap : KeyedEntityMap<WorkspaceAppUser, int>
    {
        public override void Configure(EntityTypeBuilder<WorkspaceAppUser> builder)
        {
            base.Configure(builder);

            // (Many-to-many) Workspace > AppUser

            builder
                .HasKey(pt => new { pt.WorkspaceId, pt.UserId });

            builder
                .HasOne(pt => pt.Workspace)
                .WithMany(p => p.WorkspaceUsers)
                .HasForeignKey(pt => pt.WorkspaceId);

            builder
                .HasOne(pt => pt.User)
                .WithMany(t => t.WorkspaceUsers)
                .HasForeignKey(pt => pt.UserId);
        }
    }
}
