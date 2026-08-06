using Netptune.Transfer.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class ImportSessionEntityMap : WorkspaceEntityMap<ImportSession, int>
{
    public override void Configure(EntityTypeBuilder<ImportSession> builder)
    {
        base.Configure(builder);

        builder
            .Property(session => session.PublicId)
            .IsRequired();

        builder
            .Property(session => session.Stage)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(session => session.SourceKind)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(session => session.VendorProfile)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(session => session.OriginalName)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(session => session.StorageKey)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .Property(session => session.TargetRecordType)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(session => session.TargetProjectKey)
            .HasMaxLength(64);

        builder
            .Property(session => session.TargetBoardIdentifier)
            .HasMaxLength(128);

        builder
            .Property(session => session.CreatedBy)
            .IsRequired();

        builder
            .Property(session => session.SourceProfile)
            .HasColumnType("jsonb");

        builder
            .Property(session => session.Mapping)
            .HasColumnType("jsonb");

        builder
            .Property(session => session.PreviewResult)
            .HasColumnType("jsonb");

        builder
            .Property(session => session.Result)
            .HasColumnType("jsonb");

        builder
            .Property(session => session.ProgressMessage)
            .HasMaxLength(512);

        builder
            .Property(session => session.Error)
            .HasColumnType("text");

        builder
            .Property(session => session.QuotaReleased)
            .HasDefaultValue(false);

        builder
            .HasIndex(session => session.PublicId)
            .IsUnique();

        builder
            .HasIndex(session => new
            {
                session.WorkspaceId,
                session.CreatedAt
            });

        builder
            .HasIndex(session => session.ExpiresAt);
    }
}

public sealed class ImportSessionEntryEntityMap : KeyedEntityMap<ImportSessionEntry, long>
{
    public override void Configure(EntityTypeBuilder<ImportSessionEntry> builder)
    {
        base.Configure(builder);

        builder
            .Property(entry => entry.EntityType)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(entry => entry.Operation)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(entry => entry.PreviousValues)
            .HasColumnType("jsonb");

        builder
            .HasIndex(entry => new
            {
                entry.SessionId,
                entry.Id
            });

        builder
            .HasOne(entry => entry.Session)
            .WithMany(session => session.Entries)
            .HasForeignKey(entry => entry.SessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ImportDefinitionEntityMap : WorkspaceEntityMap<ImportDefinition, int>
{
    public override void Configure(EntityTypeBuilder<ImportDefinition> builder)
    {
        base.Configure(builder);

        builder
            .Property(definition => definition.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(definition => definition.RecordType)
            .HasMaxLength(64)
            .IsRequired();

        builder
            .Property(definition => definition.VendorProfile)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(definition => definition.Mapping)
            .HasColumnType("jsonb")
            .IsRequired();

        builder
            .HasIndex(definition => new
            {
                definition.WorkspaceId,
                definition.Name
            });
    }
}
