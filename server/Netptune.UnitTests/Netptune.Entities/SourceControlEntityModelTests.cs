using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

using Netptune.Core.Entities;
using Netptune.Entities.Contexts;

using Xunit;

namespace Netptune.UnitTests.Netptune.Entities;

public class SourceControlEntityModelTests
{
    private readonly DataContext Context = new();

    private IModel Model => Context.GetService<IDesignTimeModel>().Model;

    [Fact]
    public void Entities_ShouldMapToSnakeCaseTables()
    {
        Model.FindEntityType(typeof(SourceControlConnection))!.GetTableName().Should().Be("source_control_connections");
        Model.FindEntityType(typeof(SourceControlRepo))!.GetTableName().Should().Be("source_control_repos");
        Model.FindEntityType(typeof(SourceControlRepoProject))!.GetTableName().Should().Be("source_control_repo_projects");
    }

    [Fact]
    public void Connection_ShouldBeUniquePerWorkspaceAccountIncludingCloudConnectionsWithNoBaseUrl()
    {
        var index = FindIndex("ix_source_control_connections_workspace_account");

        index.IsUnique.Should().BeTrue();
        index.GetFilter().Should().Be("NOT is_deleted", "a disconnected account can be connected again");
        index.GetAreNullsDistinct().Should().BeFalse("two cloud connections both have a null base url");

        index.Properties.Select(property => property.Name).Should().Equal(
            nameof(SourceControlConnection.WorkspaceId),
            nameof(SourceControlConnection.Provider),
            nameof(SourceControlConnection.BaseUrl),
            nameof(SourceControlConnection.ExternalAccountId));
    }

    [Fact]
    public void Connection_ShouldBeFindableByProviderAccountForWebhookRouting()
    {
        var index = FindIndex("ix_source_control_connections_provider_account");

        index.IsUnique.Should().BeFalse("one installation may be connected to several workspaces");
    }

    [Fact]
    public void Connection_ShouldStoreSecretsAsEncryptedBytes()
    {
        var connection = Model.FindEntityType(typeof(SourceControlConnection))!;

        connection.FindProperty(nameof(SourceControlConnection.Credential))!.GetColumnType().Should().Be("bytea");
        connection.FindProperty(nameof(SourceControlConnection.WebhookSecret))!.GetColumnType().Should().Be("bytea");
    }

    [Fact]
    public void Repo_ShouldBeUniquePerConnectionAndCascadeWithIt()
    {
        var index = FindIndex("ix_source_control_repos_connection_external");
        var repo = Model.FindEntityType(typeof(SourceControlRepo))!;

        var connectionKey = repo.GetForeignKeys()
            .Single(foreignKey => foreignKey.PrincipalEntityType.ClrType == typeof(SourceControlConnection));

        index.IsUnique.Should().BeTrue();
        connectionKey.DeleteBehavior.Should().Be(DeleteBehavior.Cascade);
    }

    [Fact]
    public void RepoProject_ShouldLinkARepoToAProjectOnce()
    {
        var index = FindIndex("ix_source_control_repo_projects_repo_project");

        index.IsUnique.Should().BeTrue();
    }

    private IIndex FindIndex(string name)
    {
        var index = Model.GetEntityTypes()
            .SelectMany(entityType => entityType.GetIndexes())
            .SingleOrDefault(candidate => candidate.GetDatabaseName() == name);

        index.Should().NotBeNull($"index {name} should exist");

        return index!;
    }
}
