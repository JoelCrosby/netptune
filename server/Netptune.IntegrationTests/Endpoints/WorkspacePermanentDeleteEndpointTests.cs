using System.Net;
using System.Net.Http.Json;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Netptune.Core.Colors;
using Netptune.Core.Requests;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Core.ViewModels.Workspace;
using Netptune.Entities.Contexts;

using Npgsql;

using Xunit;

namespace Netptune.IntegrationTests.Endpoints;

[Collection(WorkspaceMutationCollection.Name)]
public sealed class WorkspacePermanentDeleteEndpointTests
{
    private readonly HttpClient Client;
    private readonly NetptuneFixture Fixture;

    public WorkspacePermanentDeleteEndpointTests(NetptuneFixture fixture)
    {
        Fixture = fixture;
        Client = fixture.CreateNetptuneClient();
    }

    [Fact]
    public async Task DeletePermanent_ShouldRemoveEveryWorkspaceRow_WhenEveryWorkspaceTableHasData()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var slug = "permanent-delete-test";
        var workspace = await CreateWorkspace(slug);
        var seeded = await SeedEveryWorkspaceTable(workspace.Id);

        await UploadStoredFiles(seeded.StorageKeys);

        var rowsBeforeDelete = await CountWorkspaceRows(workspace.Id);
        var emptyBeforeDelete = rowsBeforeDelete.Where(table => table.Count == 0).Select(table => table.Table);

        emptyBeforeDelete.Should().BeEmpty(
            "every workspace-scoped table needs a seeded row, or a missed delete would go unnoticed");

        var response = await Client.DeleteAsync($"api/workspaces/permanent/{slug}", cancellationToken);
        var result = await response.Content.ReadFromJsonAsync<ClientResponse>(cancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        result.IsSuccess.Should().BeTrue();

        var rowsAfterDelete = await CountWorkspaceRows(workspace.Id);
        var populatedAfterDelete = rowsAfterDelete.Where(table => table.Count > 0).Select(table => table.Table);
        var remains = await GetRemains(workspace.Id, seeded.ServiceAccountUserId);
        var remainingStoredFiles = await GetRemainingStoredFiles(seeded.StorageKeys);

        populatedAfterDelete.Should().BeEmpty();
        remains.Workspace.Should().BeFalse();
        remains.ServiceAccountUser.Should().BeFalse();
        remainingStoredFiles.Should().BeEmpty();
    }

    private async Task<WorkspaceViewModel> CreateWorkspace(string slug)
    {
        var response = await Client.PostAsJsonAsync("api/workspaces", new AddWorkspaceRequest
        {
            Name = slug,
            Description = $"{slug} description",
            Slug = slug,
            MetaInfo = new() { Color = NamedColors.FallbackColor },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<ClientResponse<WorkspaceViewModel>>();

        result.IsSuccess.Should().BeTrue();

        return result.Payload!;
    }

    private async Task<SeededWorkspace> SeedEveryWorkspaceTable(int workspaceId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        return await WorkspaceRowSeeder.Seed(context, workspaceId, TestContext.Current.CancellationToken);
    }

    private async Task UploadStoredFiles(IReadOnlyList<string> storageKeys)
    {
        var storage = Fixture.Services.GetRequiredService<IStorageService>();

        foreach (var key in storageKeys)
        {
            using var content = new MemoryStream([1]);
            var uploadOptions = new StorageUploadOptions { Name = key, Key = key };

            await storage.UploadFileAsync(content, uploadOptions, TestContext.Current.CancellationToken);
        }
    }

    private async Task<List<string>> GetRemainingStoredFiles(IReadOnlyList<string> storageKeys)
    {
        var storage = Fixture.Services.GetRequiredService<IStorageService>();
        var remaining = new List<string>();

        foreach (var key in storageKeys)
        {
            var exists = await storage.ExistsAsync(key, TestContext.Current.CancellationToken);

            if (exists)
            {
                remaining.Add(key);
            }
        }

        return remaining;
    }

    private async Task<List<TableRowCount>> CountWorkspaceRows(int workspaceId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var tableCounts = GetWorkspaceScopedTables(context).Select(table =>
            $"SELECT '{table.Name}' AS \"Table\", count(*)::int AS \"Count\" " +
            $"FROM {table.Name} WHERE {table.WorkspaceIdColumn} = @workspace_id");
        var sql = string.Join(" UNION ALL ", tableCounts);
        var workspaceIdParameter = new NpgsqlParameter("workspace_id", workspaceId);

        return await context.Database
            .SqlQueryRaw<TableRowCount>(sql, workspaceIdParameter)
            .ToListAsync(TestContext.Current.CancellationToken);
    }

    private static List<WorkspaceScopedTable> GetWorkspaceScopedTables(DataContext context)
    {
        return context.Model.GetEntityTypes()
            .Select(entityType => (EntityType: entityType, WorkspaceId: entityType.FindProperty("WorkspaceId")))
            .Where(entry => entry.WorkspaceId is not null && entry.EntityType.GetTableName() is not null)
            .Select(entry => new WorkspaceScopedTable(
                entry.EntityType.GetTableName()!,
                entry.WorkspaceId!.GetColumnName()))
            .Distinct()
            .OrderBy(table => table.Name)
            .ToList();
    }

    private async Task<Remains> GetRemains(int workspaceId, string serviceAccountUserId)
    {
        using var scope = Fixture.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();
        var cancellationToken = TestContext.Current.CancellationToken;

        var workspaceRemains = await context.Workspaces.AnyAsync(x => x.Id == workspaceId, cancellationToken);
        var serviceAccountUserRemains = await context.AppUsers.AnyAsync(
            user => user.Id == serviceAccountUserId,
            cancellationToken);

        return new Remains(workspaceRemains, serviceAccountUserRemains);
    }

    private sealed record WorkspaceScopedTable(string Name, string WorkspaceIdColumn);

    private sealed record Remains(bool Workspace, bool ServiceAccountUser);

    private sealed class TableRowCount
    {
        public string Table { get; init; } = null!;

        public int Count { get; init; }
    }
}
