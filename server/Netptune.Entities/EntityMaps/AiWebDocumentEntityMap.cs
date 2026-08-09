using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class AiWebDocumentEntityMap : KeyedEntityMap<AiWebDocument, Guid>
{
    public override void Configure(EntityTypeBuilder<AiWebDocument> builder)
    {
        base.Configure(builder);

        builder
            .Property(document => document.RequestedUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder
            .Property(document => document.FinalUrl)
            .HasMaxLength(2048)
            .IsRequired();

        builder
            .Property(document => document.Title)
            .HasMaxLength(512);

        builder
            .Property(document => document.ContentType)
            .HasMaxLength(128);

        builder
            .Property(document => document.Content)
            .HasColumnType("text")
            .IsRequired();

        builder
            .Property(document => document.FetchedAt)
            .IsRequired();

        builder
            .Property(document => document.ExpiresAt)
            .IsRequired();

        builder
            .HasIndex(document => document.WorkspaceId);

        builder
            .HasIndex(document => document.ExpiresAt);
    }
}
