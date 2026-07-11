using Mediator;

using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;
using Netptune.Core.UnitOfWork;

namespace Netptune.JobServer.Handlers;

public sealed class SearchIndexHandler : IRequestHandler<SearchIndexEvent>
{
    private readonly IMeilisearchService SearchService;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly ILogger<SearchIndexHandler> Logger;

    public SearchIndexHandler(IMeilisearchService searchService, INetptuneUnitOfWork unitOfWork, ILogger<SearchIndexHandler> logger)
    {
        SearchService = searchService;
        UnitOfWork = unitOfWork;
        Logger = logger;
    }

    public async ValueTask<Unit> Handle(SearchIndexEvent request, CancellationToken cancellationToken)
    {
        if (request.EntityIds.Count == 0) return default;

        if (request.Operation == SearchIndexOperation.Delete)
        {
            var indexName = GetIndexName(request.EntityType);
            var documentIds = request.EntityIds.Select(id => GetDocumentId(request.EntityType, id));

            await SearchService.DeleteDocumentsAsync(indexName, documentIds, cancellationToken);
            Logger.LogInformation("[Search] deleted {Count} {Type}(s)", request.EntityIds.Count, request.EntityType);

            return default;
        }

        await IndexEntitiesAsync(request, cancellationToken);
        Logger.LogInformation("[Search] indexed {Count} {Type}(s)", request.EntityIds.Count, request.EntityType);

        return default;
    }

    private async Task IndexEntitiesAsync(SearchIndexEvent request, CancellationToken ct)
    {
        switch (request.EntityType)
        {
            case "task":
                await IndexTasksAsync(request.EntityIds, request.WorkspaceSlug, ct);
                break;
            case "project":
                await IndexProjectsAsync(request.EntityIds, request.WorkspaceSlug, ct);
                break;
            case "board":
                await IndexBoardsAsync(request.EntityIds, request.WorkspaceSlug, ct);
                break;
            case "sprint":
                await IndexSprintsAsync(request.EntityIds, request.WorkspaceSlug, ct);
                break;
        }
    }

    private async Task IndexTasksAsync(IReadOnlyList<int> taskIds, string workspaceSlug, CancellationToken ct)
    {
        var tasks = await UnitOfWork.Tasks.GetTaskViewModels(taskIds, ct);

        if (tasks.Count == 0) return;

        var documents = tasks.Select(task => task.ToSearchDocument(workspaceSlug));

        await SearchService.IndexDocumentsAsync("tasks", documents, ct);
    }

    private async Task IndexProjectsAsync(IReadOnlyList<int> projectIds, string workspaceSlug, CancellationToken ct)
    {
        var projects = await UnitOfWork.Projects.GetProjectViewModels(projectIds, ct);

        var documents = projects
            .Select(project => new ProjectSearchDocument
            {
                Id = $"project_{project.Id}",
                ProjectId = project.Id,
                Name = project.Name,
                Description = project.Description,
                Key = project.Key,
                WorkspaceSlug = workspaceSlug,
                UpdatedAt = project.UpdatedAt ?? project.CreatedAt,
            })
            .ToList();

        if (documents.Count == 0) return;

        await SearchService.IndexDocumentsAsync("projects", documents, ct);
    }

    private async Task IndexBoardsAsync(IReadOnlyList<int> boardIds, string workspaceSlug, CancellationToken ct)
    {
        var boards = await UnitOfWork.Boards.GetBoardsById(boardIds, ct);

        var documents = boards
            .Select(board => new BoardSearchDocument
            {
                Id = $"board_{board.Id}",
                BoardId = board.Id,
                Name = board.Name,
                WorkspaceSlug = workspaceSlug,
                ProjectId = board.ProjectId,
                Identifier = board.Identifier,
                UpdatedAt = board.UpdatedAt ?? board.CreatedAt,
            })
            .ToList();

        if (documents.Count == 0) return;

        await SearchService.IndexDocumentsAsync("boards", documents, ct);
    }

    private async Task IndexSprintsAsync(IReadOnlyList<int> sprintIds, string workspaceSlug, CancellationToken ct)
    {
        var sprints = await UnitOfWork.Sprints.GetSprintViewModels(sprintIds, ct);

        var documents = sprints
            .Select(sprint => new SprintSearchDocument
            {
                Id = $"sprint_{sprint.Id}",
                SprintId = sprint.Id,
                Name = sprint.Name,
                Goal = sprint.Goal,
                WorkspaceSlug = workspaceSlug,
                ProjectId = sprint.ProjectId,
                Status = sprint.Status.ToString(),
                UpdatedAt = sprint.UpdatedAt ?? sprint.CreatedAt,
            })
            .ToList();

        if (documents.Count == 0) return;

        await SearchService.IndexDocumentsAsync("sprints", documents, ct);
    }

    private static string GetIndexName(string entityType)
    {
        return entityType switch
        {
            "task" => "tasks",
            "project" => "projects",
            "board" => "boards",
            "sprint" => "sprints",
            _ => throw new ArgumentException($"Unknown entity type: {entityType}", nameof(entityType)),
        };
    }

    private static string GetDocumentId(string entityType, int entityId)
    {
        return $"{entityType}_{entityId}";
    }
}
