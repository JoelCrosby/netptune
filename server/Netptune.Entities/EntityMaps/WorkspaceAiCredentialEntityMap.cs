using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class WorkspaceAiCredentialEntityMap : KeyedEntityMap<WorkspaceAiCredential, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkspaceAiCredential> builder)
    {
        base.Configure(builder);

        builder
            .Property(credential => credential.WorkspaceId)
            .IsRequired();

        builder
            .Property(credential => credential.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(credential => credential.Label)
            .HasMaxLength(128)
            .IsRequired();

        builder
            .Property(credential => credential.Secret)
            .HasColumnType("bytea")
            .IsRequired();

        builder
            .Property(credential => credential.SecretHint)
            .HasMaxLength(8)
            .IsRequired();

        builder
            .Property(credential => credential.Model)
            .HasMaxLength(128);

        builder
            .Property(credential => credential.CreatedAt)
            .IsRequired();

        builder
            .HasIndex(credential => new { credential.WorkspaceId, credential.Provider })
            .IsUnique();

        builder
            .HasOne(credential => credential.Workspace)
            .WithMany()
            .HasForeignKey(credential => credential.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
