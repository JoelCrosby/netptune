using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class WorkspaceSearchCredentialEntityMap : KeyedEntityMap<WorkspaceSearchCredential, Guid>
{
    public override void Configure(EntityTypeBuilder<WorkspaceSearchCredential> builder)
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
            .Property(credential => credential.Secret)
            .HasColumnType("bytea");

        builder
            .Property(credential => credential.SecretHint)
            .HasMaxLength(8)
            .IsRequired();

        builder
            .Property(credential => credential.EngineId)
            .HasMaxLength(128);

        builder
            .Property(credential => credential.Endpoint)
            .HasMaxLength(2048);

        builder
            .Property(credential => credential.CreatedAt)
            .IsRequired();

        builder
            .HasIndex(credential => credential.WorkspaceId)
            .IsUnique();

        builder
            .HasOne(credential => credential.Workspace)
            .WithMany()
            .HasForeignKey(credential => credential.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
