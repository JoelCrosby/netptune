using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public class UserPreferenceValueEntityMap : KeyedEntityMap<UserPreferenceValue, int>
{
    public override void Configure(EntityTypeBuilder<UserPreferenceValue> builder)
    {
        base.Configure(builder);

        builder
            .Property(value => value.UserId)
            .IsRequired();

        builder
            .Property(value => value.Key)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(value => value.Value)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .HasIndex(value => new { value.UserId, value.Key })
            .IsUnique()
            .HasFilter("workspace_id IS NULL")
            .HasDatabaseName("ix_user_preference_values_user_key_global");

        builder
            .HasIndex(value => new { value.UserId, value.WorkspaceId, value.Key })
            .IsUnique()
            .HasFilter("workspace_id IS NOT NULL")
            .HasDatabaseName("ix_user_preference_values_user_workspace_key");

        builder
            .HasOne(value => value.User)
            .WithMany()
            .HasForeignKey(value => value.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(value => value.Workspace)
            .WithMany()
            .HasForeignKey(value => value.WorkspaceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
