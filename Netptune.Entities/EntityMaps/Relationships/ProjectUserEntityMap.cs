using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships
{
    public class ProjectUserEntityMap : KeyedEntityMap<ProjectUser, int>
    {
        public override void Configure(EntityTypeBuilder<ProjectUser> builder)
        {
            base.Configure(builder);

            // (Many-to-many) Project > AppUser

            builder
                .HasAlternateKey(projectUser => new { projectUser.ProjectId, projectUser.UserId });

            builder
                .HasOne(projectUser => projectUser.Project)
                .WithMany(project => project.ProjectUsers)
                .HasForeignKey(projectUser => projectUser.ProjectId);

            builder
                .HasOne(projectUser => projectUser.User)
                .WithMany(user => user.ProjectUsers)
                .HasForeignKey(projectUser => projectUser.UserId);
        }
    }
}
