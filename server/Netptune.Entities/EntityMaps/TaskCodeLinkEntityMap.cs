using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Netptune.Core.Entities;
using Netptune.Entities.EntityMaps.BaseMaps;

namespace Netptune.Entities.EntityMaps;

public sealed class TaskCodeLinkEntityMap : KeyedEntityMap<TaskCodeLink, long>
{
    public override void Configure(EntityTypeBuilder<TaskCodeLink> builder)
    {
        base.Configure(builder);

        builder
            .Property(link => link.Kind)
            .HasConversion<int>()
            .IsRequired();

        builder
            .Property(link => link.ExternalRef)
            .HasMaxLength(256)
            .IsRequired();

        builder
            .Property(link => link.Title)
            .HasMaxLength(512)
            .IsRequired();

        builder
            .Property(link => link.WebUrl)
            .HasMaxLength(1024)
            .IsRequired();

        builder
            .Property(link => link.AuthorLogin)
            .HasMaxLength(256);

        builder
            .Property(link => link.PullRequestState)
            .HasConversion<int?>();

        builder
            .Property(link => link.HeadSha)
            .HasMaxLength(64);

        builder
            .HasOne(link => link.Repo)
            .WithMany()
            .HasForeignKey(link => link.RepoId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasOne(link => link.Task)
            .WithMany()
            .HasForeignKey(link => link.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(link => new { link.TaskId, link.RepoId, link.Kind, link.ExternalRef })
            .IsUnique()
            .HasDatabaseName("ix_task_code_links_task_repo_kind_ref");

        builder
            .HasIndex(link => new { link.RepoId, link.Kind, link.ExternalRef })
            .HasDatabaseName("ix_task_code_links_repo_kind_ref");
    }
}
