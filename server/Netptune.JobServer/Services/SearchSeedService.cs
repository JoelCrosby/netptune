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
    private readonly IHostEnvironment Environment;
    private readonly ILogger<SearchSeedService> Logger;

    public SearchSeedService(
        IMeilisearchService searchService,
        MeilisearchClient client,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger<SearchSeedService> logger)
    {
        SearchService = searchService;
        Client = client;
        ScopeFactory = scopeFactory;
        Environment = environment;
        Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!Environment.IsDevelopment()) return;

        await SearchService.EnsureIndexSettingsAsync(stoppingToken);

        if (!await NeedsSeedingAsync(stoppingToken)) return;

        Logger.LogInformation("[Search] seeding indexes");

        using var scope = ScopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INetptuneUnitOfWork>();

        var workspaces = await unitOfWork.Workspaces.GetWorkspaces(stoppingToken);
        var slugs = workspaces.Select(w => w.Slug).ToList();

        await IndexTasksAsync(unitOfWork, slugs, stoppingToken);
        await IndexProjectsAsync(unitOfWork, slugs, stoppingToken);
        await IndexBoardsAsync(unitOfWork, slugs, stoppingToken);
        await IndexSprintsAsync(unitOfWork, slugs, stoppingToken);

        Logger.LogInformation("[Search] seeding complete");
    }

    private async Task<bool> NeedsSeedingAsync(CancellationToken ct)
    {
        try
        {
            var stats = await Client.Index("tasks").GetStatsAsync(ct);
            return stats.NumberOfDocuments == 0;
        }
        catch (MeilisearchApiError ex) when (ex.Code == "index_not_found")
        {
            return true;
        }
    }

    private async Task IndexTasksAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<TaskSearchDocument>();

        foreach (var slug in slugs)
        {
            var tasks = await unitOfWork.Tasks.GetTasksAsync(slug, cancellationToken: ct);

            docs.AddRange(tasks.Select(t => new TaskSearchDocument
            {
                Id = $"task_{t.Id}",
                TaskId = t.Id,
                Title = t.Name,
                Description = t.Description,
                SystemId = t.SystemId,
                WorkspaceSlug = slug,
                Status = t.Status.ToString(),
                Priority = t.Priority?.ToString(),
                AssigneeIds = t.Assignees.Select(a => a.Id).ToList(),
                ProjectId = t.ProjectId,
                UpdatedAt = (t.UpdatedAt ?? t.CreatedAt).UtcDateTime,
            }));
        }

        foreach (var batch in docs.Chunk(BatchSize))
        {
            await SearchService.IndexDocumentsAsync("tasks", batch, ct);
        }
    }

    private async Task IndexProjectsAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<ProjectSearchDocument>();

        foreach (var slug in slugs)
        {
            var projects = await unitOfWork.Projects.GetProjects(slug, ct);

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

        foreach (var batch in docs.Chunk(BatchSize))
        {
            await SearchService.IndexDocumentsAsync("projects", batch, ct);
        }
    }

    private async Task IndexBoardsAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<BoardSearchDocument>();

        foreach (var slug in slugs)
        {
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

        foreach (var batch in docs.Chunk(BatchSize))
        {
            await SearchService.IndexDocumentsAsync("boards", batch, ct);
        }
    }

    private async Task IndexSprintsAsync(INetptuneUnitOfWork unitOfWork, List<string> slugs, CancellationToken ct)
    {
        var docs = new List<SprintSearchDocument>();

        foreach (var slug in slugs)
        {
            var sprints = await unitOfWork.Sprints.GetSprintsAsync(slug, cancellationToken: ct);

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

        foreach (var batch in docs.Chunk(BatchSize))
        {
            await SearchService.IndexDocumentsAsync("sprints", batch, ct);
        }
    }
}
