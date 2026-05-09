using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Relationships;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps.Relationships;

public class WorkspaceInviteEntityMap : KeyedEntityMap<WorkspaceInvite, int>
{
    public override void Configure(EntityTypeBuilder<WorkspaceInvite> builder)
    {
        base.Configure(builder);

        builder.HasIndex(x => x.Code).IsUnique();

        builder.HasIndex(x => new { x.Email, x.WorkspaceId, x.AcceptedAt })
            .HasDatabaseName("ix_workspace_invites_email_workspace_accepted");

        builder
            .HasOne(x => x.Workspace)
            .WithMany()
            .HasForeignKey(x => x.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(x => x.InvitedBy)
            .WithMany()
            .HasForeignKey(x => x.InvitedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(x => x.Email).HasMaxLength(256).IsRequired();
        builder.Property(x => x.Code).HasMaxLength(64).IsRequired();
    }
}
