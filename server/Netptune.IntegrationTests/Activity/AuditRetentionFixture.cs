using System.Collections.Concurrent;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Netptune.Core.Entities;
using Netptune.Core.Responses;
using Netptune.Core.Responses.Common;
using Netptune.Core.Services;
using Netptune.Core.Storage;
using Netptune.Entities.Configuration;
using Netptune.Entities.Contexts;

using Testcontainers.PostgreSql;

using Xunit;

namespace Netptune.IntegrationTests.Activity;

// Deliberately separate from NetptuneFixture's database: the shared seed data is calendar-anchored
// (EventRecordSeeder.BaseDate is 2025-01-01), so a growing slice of it is already past the retention cutoff
// and running the real job against it would delete the rows the endpoint tests read.
public sealed class AuditRetentionFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer DbContainer = new PostgreSqlBuilder("postgres:18.3").Build();

    private ServiceProvider Provider = null!;

    public CountingStorageService Storage { get; } = new();

    public IServiceScopeFactory ScopeFactory => Provider.GetRequiredService<IServiceScopeFactory>();

    public string UserId { get; private set; } = null!;

    public int WorkspaceId { get; private set; }

    public async ValueTask InitializeAsync()
    {
        await DbContainer.StartAsync();

        var services = new ServiceCollection();

        services.AddLogging(logging => logging.SetMinimumLevel(LogLevel.Warning));
        services.AddNetptuneEntities(options => options.ConnectionString = DbContainer.GetConnectionString());
        services.AddSingleton<IStorageService>(Storage);

        Provider = services.BuildServiceProvider();

        using var scope = Provider.CreateScope();

        var db = scope.ServiceProvider.GetRequiredService<DataContext>();

        await db.Database.EnsureCreatedAsync();

        var user = new AppUser
        {
            UserName = "audit@netptune.co.uk",
            Email = "audit@netptune.co.uk",
            Firstname = "Audit",
            Lastname = "Retention",
        };

        var workspace = new Workspace
        {
            Name = "Audit",
            Slug = "audit",
            CreatedAt = DateTime.UtcNow,
            MetaInfo = new(),
        };

        db.Users.Add(user);
        db.Workspaces.Add(workspace);

        await db.SaveChangesAsync();

        UserId = user.Id;
        WorkspaceId = workspace.Id;
    }

    public IServiceScope CreateScope() => Provider.CreateScope();

    public async ValueTask DisposeAsync()
    {
        await Provider.DisposeAsync();
        await DbContainer.DisposeAsync();
    }
}

public sealed class CountingStorageService : IStorageService
{
    private int Failing;

    public ConcurrentBag<string> Uploads { get; } = [];

    public ConcurrentBag<string> ArchivedLines { get; } = [];

    public IDisposable FailUploads()
    {
        Interlocked.Exchange(ref Failing, 1);

        return new FailureScope(this);
    }

    public async Task<ClientResponse<UploadResponse>> UploadFileAsync(Stream stream, StorageUploadOptions uploadOptions, CancellationToken cancellationToken = default)
    {
        Uploads.Add(uploadOptions.Key);

        using var reader = new StreamReader(stream, leaveOpen: true);

        var body = await reader.ReadToEndAsync();
        var size = stream.Length;

        if (Volatile.Read(ref Failing) == 1)
        {
            return ClientResponse<UploadResponse>.Failed();
        }

        foreach (var line in body.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            ArchivedLines.Add(line);
        }

        return ClientResponse<UploadResponse>.Success(new UploadResponse
        {
            Name = uploadOptions.Name,
            Key = uploadOptions.Key,
            Path = uploadOptions.Key,
            Size = size,
            Uri = $"https://bucket.s3.region.suffix/{uploadOptions.Key}",
        });
    }

    public Task<Uri?> GetReadUriAsync(StorageReadOptions readOptions, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Uri?>(new Uri($"https://storage.test/{readOptions.Key}"));
    }

    public Task DeleteFileAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Uploads.Contains(key));
    }

    private sealed class FailureScope(CountingStorageService storage) : IDisposable
    {
        public void Dispose() => Interlocked.Exchange(ref storage.Failing, 0);
    }
}
