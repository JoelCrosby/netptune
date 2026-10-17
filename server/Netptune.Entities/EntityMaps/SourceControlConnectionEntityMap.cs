using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class SourceControlConnectionEntityMap : WorkspaceEntityMap<SourceControlConnection, int>
{
    public override void Configure(EntityTypeBuilder<SourceControlConnection> builder)
    {
        base.Configure(builder);

        builder
            .Property(connection => connection.Provider)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(connection => connection.BaseUrl)
            .HasMaxLength(512);

        builder
            .Property(connection => connection.ExternalAccountId)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(connection => connection.AccountLogin)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(connection => connection.AccountType)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(connection => connection.AccountAvatarUrl)
            .HasMaxLength(1024);

        builder
            .Property(connection => connection.Status)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(connection => connection.GrantedCapabilities)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(connection => connection.CredentialKind)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(connection => connection.Credential)
            .HasColumnType("bytea");

        builder
            .Property(connection => connection.CredentialHint)
            .HasMaxLength(8);

        builder
            .Property(connection => connection.WebhookSecret)
            .HasColumnType("bytea");

        builder
            .HasIndex(connection => new
            {
                connection.WorkspaceId,
                connection.Provider,
                connection.BaseUrl,
                connection.ExternalAccountId,
            })
            .IsUnique()
            .AreNullsDistinct(false)
            .HasFilter("NOT is_deleted")
            .HasDatabaseName("ix_source_control_connections_workspace_account");

        builder
            .HasIndex(connection => new { connection.Provider, connection.ExternalAccountId })
            .HasDatabaseName("ix_source_control_connections_provider_account");
    }
}
