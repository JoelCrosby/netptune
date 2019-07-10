using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Netptune.Entities.Entites.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps
{
    public class ProjectUserEntityMap : KeyedEntityMap<ProjectUser, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectUser> builder)
        {
            base.Configure(builder);

            // (Many-to-many) Project > AppUser

            builder
                .HasKey(pt => new { pt.ProjectId, pt.UserId });

            builder
                .HasOne(pt => pt.Project)
                .WithMany(p => p.ProjectUsers)
                .HasForeignKey(pt => pt.ProjectId);

            builder
                .HasOne(pt => pt.User)
                .WithMany(t => t.ProjectUsers)
                .HasForeignKey(pt => pt.UserId);
        }
    }
}
