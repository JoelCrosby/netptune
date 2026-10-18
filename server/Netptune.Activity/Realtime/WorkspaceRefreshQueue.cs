using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Netptune.Core.Services.Realtime;
using Netptune.Entities.Contexts;

namespace Netptune.Activity.Realtime;

// Background work can change hundreds of tasks in a burst, so pushes are merged per workspace and sent at
// most once per interval rather than once per event.
public sealed class WorkspaceRefreshQueue : BackgroundService, IWorkspaceRefreshQueue
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromMilliseconds(500);

    private readonly Lock Gate = new();
    private readonly IServiceScopeFactory ScopeFactory;
    private readonly IWorkspaceEventPublisher Publisher;
    private readonly ILogger<WorkspaceRefreshQueue> Logger;
    private Dictionary<int, HashSet<string>> Pending = [];

    public WorkspaceRefreshQueue(
        IServiceScopeFactory scopeFactory,
        IWorkspaceEventPublisher publisher,
        ILogger<WorkspaceRefreshQueue> logger)
    {
        ScopeFactory = scopeFactory;
        Publisher = publisher;
        Logger = logger;
    }

    public void Enqueue(int workspaceId, IReadOnlyCollection<string> scopes)
    {
        if (scopes.Count == 0)
        {
            return;
        }

        lock (Gate)
        {
            var pendingScopes = Pending.GetValueOrDefault(workspaceId) ?? [];

            pendingScopes.UnionWith(scopes);
            Pending[workspaceId] = pendingScopes;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await Flush(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                Logger.LogWarning(exception, "Publishing background workspace changes failed");
            }
        }
    }

    private async Task Flush(CancellationToken cancellationToken)
    {
        var batch = TakePending();

        if (batch.Count == 0)
        {
            return;
        }

        var slugs = await GetWorkspaceSlugs(batch.Keys.ToList(), cancellationToken);

        foreach (var (workspaceId, scopes) in batch)
        {
            if (!slugs.TryGetValue(workspaceId, out var slug))
            {
                continue;
            }

            await Publisher.PublishAsync(slug, string.Empty, scopes.ToArray());
        }
    }

    private Dictionary<int, HashSet<string>> TakePending()
    {
        lock (Gate)
        {
            var batch = Pending;

            Pending = [];

            return batch;
        }
    }

    private async Task<Dictionary<int, string>> GetWorkspaceSlugs(
        List<int> workspaceIds,
        CancellationToken cancellationToken)
    {
        using var scope = ScopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DataContext>();

        var slugs = await context.Workspaces
            .AsNoTracking()
            .Where(workspace => workspaceIds.Contains(workspace.Id))
            .Select(workspace => new { workspace.Id, workspace.Slug })
            .ToDictionaryAsync(workspace => workspace.Id, workspace => workspace.Slug, cancellationToken);

        return slugs;
    }
}
