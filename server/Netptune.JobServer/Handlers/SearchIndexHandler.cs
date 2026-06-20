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
        if (request.Operation == SearchIndexOperation.Delete)
        {
            var (indexName, docId) = GetIndexAndId(request.EntityType, request.EntityId);
            await SearchService.DeleteDocumentAsync(indexName, docId, cancellationToken);
            Logger.LogInformation("[Search] deleted {Type} {Id}", request.EntityType, request.EntityId);
            return default;
        }

        await IndexEntityAsync(request, cancellationToken);
        Logger.LogInformation("[Search] indexed {Type} {Id}", request.EntityType, request.EntityId);

        return default;
    }

    private async Task IndexEntityAsync(SearchIndexEvent request, CancellationToken ct)
    {
        switch (request.EntityType)
        {
            case "task":
                await IndexTaskAsync(request.EntityId, request.WorkspaceSlug, ct);
                break;
            case "project":
                await IndexProjectAsync(request.EntityId, request.WorkspaceSlug, ct);
                break;
            case "board":
                await IndexBoardAsync(request.EntityId, request.WorkspaceSlug, ct);
                break;
            case "sprint":
                await IndexSprintAsync(request.EntityId, request.WorkspaceSlug, ct);
                break;
        }
    }

    private async Task IndexTaskAsync(int taskId, string workspaceSlug, CancellationToken ct)
    {
        var task = await UnitOfWork.Tasks.GetAsync(taskId, cancellationToken: ct);
        if (task is null) return;

        var doc = new TaskSearchDocument
        {
            Id = $"task_{task.Id}",
            TaskId = task.Id,
            Title = task.Name,
            Description = task.Description,
            SystemId = $"{task.ProjectScopeId}",
            WorkspaceSlug = workspaceSlug,
            Status = task.Status.Name,
            Priority = task.Priority?.ToString(),
            ProjectId = task.ProjectId,
            UpdatedAt = task.UpdatedAt ?? task.CreatedAt,
        };

        await SearchService.IndexDocumentsAsync("tasks", [doc], ct);
    }

    private async Task IndexProjectAsync(int projectId, string workspaceSlug, CancellationToken ct)
    {
        var projects = await UnitOfWork.Projects.GetProjects(workspaceSlug, ct);
        var project = projects.FirstOrDefault(p => p.Id == projectId);
        if (project is null) return;

        var doc = new ProjectSearchDocument
        {
            Id = $"project_{project.Id}",
            ProjectId = project.Id,
            Name = project.Name,
            Description = project.Description,
            Key = project.Key,
            WorkspaceSlug = workspaceSlug,
            UpdatedAt = project.UpdatedAt ?? project.CreatedAt,
        };

        await SearchService.IndexDocumentsAsync("projects", [doc], ct);
    }

    private async Task IndexBoardAsync(int boardId, string workspaceSlug, CancellationToken ct)
    {
        var boards = await UnitOfWork.Boards.GetBoards(workspaceSlug, isReadonly: true, ct);
        var board = boards.FirstOrDefault(b => b.Id == boardId);
        if (board is null) return;

        var doc = new BoardSearchDocument
        {
            Id = $"board_{board.Id}",
            BoardId = board.Id,
            Name = board.Name,
            WorkspaceSlug = workspaceSlug,
            ProjectId = board.ProjectId,
            Identifier = board.Identifier,
            UpdatedAt = board.UpdatedAt ?? board.CreatedAt,
        };

        await SearchService.IndexDocumentsAsync("boards", [doc], ct);
    }

    private async Task IndexSprintAsync(int sprintId, string workspaceSlug, CancellationToken ct)
    {
        var sprints = await UnitOfWork.Sprints.GetSprintsAsync(workspaceSlug, cancellationToken: ct);
        var sprint = sprints.FirstOrDefault(s => s.Id == sprintId);
        if (sprint is null) return;

        var doc = new SprintSearchDocument
        {
            Id = $"sprint_{sprint.Id}",
            SprintId = sprint.Id,
            Name = sprint.Name,
            Goal = sprint.Goal,
            WorkspaceSlug = workspaceSlug,
            ProjectId = sprint.ProjectId,
            Status = sprint.Status.ToString(),
            UpdatedAt = sprint.UpdatedAt ?? sprint.CreatedAt,
        };

        await SearchService.IndexDocumentsAsync("sprints", [doc], ct);
    }

    private static (string indexName, string docId) GetIndexAndId(string entityType, int entityId)
    {
        return entityType switch
        {
            "task" => ("tasks", $"task_{entityId}"),
            "project" => ("projects", $"project_{entityId}"),
            "board" => ("boards", $"board_{entityId}"),
            "sprint" => ("sprints", $"sprint_{entityId}"),
            _ => throw new ArgumentException($"Unknown entity type: {entityType}", nameof(entityType)),
        };
    }
}
