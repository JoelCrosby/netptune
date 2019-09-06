using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;
using Netptune.Models.Relationships;

namespace Netptune.Entities.EntityMaps
{
    public class WorkspaceAppUserEntityMap : KeyedEntityMap<WorkspaceAppUser, int>
    {
        public override void Configure(EntityTypeBuilder<WorkspaceAppUser> builder)
        {
            base.Configure(builder);

            // (Many-to-many) Workspace > AppUser

            builder
                .HasKey(workspaceUser => new { workspaceUser.WorkspaceId, workspaceUser.UserId });

            builder
                .HasOne(workspaceUser => workspaceUser.Workspace)
                .WithMany(workspace => workspace.WorkspaceUsers)
                .HasForeignKey(workspaceUser => workspaceUser.WorkspaceId);

            builder
                .HasOne(workspaceUser => workspaceUser.User)
                .WithMany(user => user.WorkspaceUsers)
                .HasForeignKey(workspaceUser => workspaceUser.UserId);
        }
    }
}
