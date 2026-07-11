using System.Diagnostics;

using Meilisearch;

using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;
using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Services;

public sealed class SearchSeedService : BackgroundService
{
    private const int BatchSize = 1000;

    private readonly IMeilisearchService SearchService;
    private readonly MeilisearchClient Client;
    private readonly IServiceScopeFactory ScopeFactory;
    private readonly ILogger<SearchSeedService> Logger;

    public SearchSeedService(
        IMeilisearchService searchService,
        MeilisearchClient client,
        IServiceScopeFactory scopeFactory,
        ILogger<SearchSeedService> logger)
    {
        SearchService = searchService;
        Client = client;
        ScopeFactory = scopeFactory;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var timer = Stopwatch.StartNew();

        Logger.LogInformation("[Search] seed service starting");

        try
        {
            Logger.LogInformation("[Search] ensuring index settings");
            await SearchService.EnsureIndexSettingsAsync(stoppingToken);

            if (!await NeedsSeedingAsync(stoppingToken))
            {
                Logger.LogInformation("[Search] indexes already contain documents, skipping seed");
                return;
            }

            Logger.LogInformation("[Search] seeding indexes");

            using var scope = ScopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();

            var workspaces = await unitOfWork.Workspaces.GetWorkspaces(stoppingToken);
            var slugs = workspaces.Select(w => w.Slug).ToList();

            Logger.LogInformation("[Search] found {WorkspaceCount} workspaces for seeding", slugs.Count);

            var taskCount = await SeedIndexAsync("tasks", () => IndexTasksAsync(unitOfWork, slugs, stoppingToken), stoppingToken);
            var projectCount = await SeedIndexAsync("projects", () => IndexProjectsAsync(unitOfWork, slugs, stoppingToken), stoppingToken);
            var boardCount = await SeedIndexAsync("boards", () => IndexBoardsAsync(unitOfWork, slugs, stoppingToken), stoppingToken);
            var sprintCount = await SeedIndexAsync("sprints", () => IndexSprintsAsync(unitOfWork, slugs, stoppingToken), stoppingToken);

            Logger.LogInformation(
                "[Search] seeding complete in {ElapsedMs}ms: {TaskCount} tasks, {ProjectCount} projects, {BoardCount} boards, {SprintCount} sprints",
                timer.ElapsedMilliseconds,
                taskCount,
                projectCount,
                boardCount,
                sprintCount);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            Logger.LogWarning("[Search] seeding cancelled after {ElapsedMs}ms", timer.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Search] seeding failed after {ElapsedMs}ms", timer.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task<bool> NeedsSeedingAsync(CancellationToken ct)
    {
        try
        {
            var stats = await Client.Index("tasks").GetStatsAsync(ct);
            Logger.LogInformation("[Search] tasks index contains {DocumentCount} documents", stats.NumberOfDocuments);
            return stats.NumberOfDocuments == 0;
        }
        catch (MeilisearchApiError ex) when (ex.Code == "index_not_found")
        {
            Logger.LogInformation("[Search] tasks index does not exist, seeding required");
            return true;
        }
    }

    private async Task<int> SeedIndexAsync(string indexName, Func<Task<int>> seedIndex, CancellationToken ct)
    {
        var timer = Stopwatch.StartNew();

        Logger.LogInformation("[Search] seeding {IndexName} index", indexName);

        try
        {
            var count = await seedIndex();

            Logger.LogInformation(
                "[Search] seeded {IndexName} index with {DocumentCount} documents in {ElapsedMs}ms",
                indexName,
                count,
                timer.ElapsedMilliseconds);

            return count;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[Search] failed seeding {IndexName} index after {ElapsedMs}ms", indexName, timer.ElapsedMilliseconds);
            throw;
        }
    }

    private async Task IndexBatchesAsync<T>(string indexName, IReadOnlyCollection<T> docs, CancellationToken ct)
    {
        if (docs.Count == 0)
        {
            Logger.LogInformation("[Search] no documents to index for {IndexName}", indexName);
            return;
        }

        var totalBatches = (int)Math.Ceiling((double)docs.Count / BatchSize);
        var batchNumber = 0;
        var indexed = 0;

        foreach (var batch in docs.Chunk(BatchSize))
        {
            batchNumber++;

            try
            {
                await SearchService.IndexDocumentsAsync(indexName, batch, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "[Search] failed indexing batch {BatchNumber}/{TotalBatches} for {IndexName}; {IndexedCount}/{DocumentCount} documents indexed",
                    batchNumber,
                    totalBatches,
                    indexName,
                    indexed,
                    docs.Count);

                throw;
            }

            indexed += batch.Length;

            Logger.LogInformation(
                "[Search] indexed batch {BatchNumber}/{TotalBatches} for {IndexName}; {IndexedCount}/{DocumentCount} documents indexed",
                batchNumber,
                totalBatches,
                indexName,
                indexed,
                docs.Count);
        }
    }

    private async Task<int> IndexTasksAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<TaskSearchDocument>();

        foreach (var slug in slugs)
        {
            Logger.LogInformation("[Search] loading task documents for workspace {WorkspaceSlug}", slug);
            var tasks = await unitOfWork.Tasks.GetAllTaskViewModels(slug, ct);

            docs.AddRange(tasks.Select(task => task.ToSearchDocument(slug)));
        }

        Logger.LogInformation("[Search] loaded {DocumentCount} task documents", docs.Count);
        await IndexBatchesAsync("tasks", docs, ct);

        return docs.Count;
    }

    private async Task<int> IndexProjectsAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<ProjectSearchDocument>();

        foreach (var slug in slugs)
        {
            Logger.LogInformation("[Search] loading project documents for workspace {WorkspaceSlug}", slug);
            var projects = await unitOfWork.Projects.GetAllProjectViewModels(slug, ct);

            docs.AddRange(projects.Select(p => new ProjectSearchDocument
            {
                Id = $"project_{p.Id}",
                ProjectId = p.Id,
                Name = p.Name,
                Description = p.Description,
                Key = p.Key,
                WorkspaceSlug = slug,
                UpdatedAt = p.UpdatedAt ?? p.CreatedAt,
            }));
        }

        Logger.LogInformation("[Search] loaded {DocumentCount} project documents", docs.Count);
        await IndexBatchesAsync("projects", docs, ct);

        return docs.Count;
    }

    private async Task<int> IndexBoardsAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<BoardSearchDocument>();

        foreach (var slug in slugs)
        {
            Logger.LogInformation("[Search] loading board documents for workspace {WorkspaceSlug}", slug);
            var boards = await unitOfWork.Boards.GetBoards(slug, isReadonly: true, ct);

            docs.AddRange(boards.Select(b => new BoardSearchDocument
            {
                Id = $"board_{b.Id}",
                BoardId = b.Id,
                Name = b.Name,
                WorkspaceSlug = slug,
                ProjectId = b.ProjectId,
                Identifier = b.Identifier,
                UpdatedAt = b.UpdatedAt ?? b.CreatedAt,
            }));
        }

        Logger.LogInformation("[Search] loaded {DocumentCount} board documents", docs.Count);
        await IndexBatchesAsync("boards", docs, ct);

        return docs.Count;
    }

    private async Task<int> IndexSprintsAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<SprintSearchDocument>();

        foreach (var slug in slugs)
        {
            Logger.LogInformation("[Search] loading sprint documents for workspace {WorkspaceSlug}", slug);
            var sprints = await unitOfWork.Sprints.GetAllSprintViewModels(slug, ct);

            docs.AddRange(sprints.Select(s => new SprintSearchDocument
            {
                Id = $"sprint_{s.Id}",
                SprintId = s.Id,
                Name = s.Name,
                Goal = s.Goal,
                WorkspaceSlug = slug,
                ProjectId = s.ProjectId,
                Status = s.Status.ToString(),
                UpdatedAt = s.UpdatedAt ?? s.CreatedAt,
            }));
        }

        Logger.LogInformation("[Search] loaded {DocumentCount} sprint documents", docs.Count);
        await IndexBatchesAsync("sprints", docs, ct);

        return docs.Count;
    }
}
