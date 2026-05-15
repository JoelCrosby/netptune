using Mediator;

using Netptune.Core.Models.Search;
using Netptune.Core.Services;
using Netptune.Core.Services.Search;
using Netptune.Core.UnitOfWork;

namespace Netptune.Handlers.Search;

public sealed record ReindexCommand : IRequest;

public sealed class ReindexCommandHandler : IRequestHandler<ReindexCommand>
{
    private const int BatchSize = 1000;

    private readonly IMeilisearchService Search;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;

    public ReindexCommandHandler(IMeilisearchService search, INetptuneUnitOfWork unitOfWork, IIdentityService identity)
    {
        Search = search;
        UnitOfWork = unitOfWork;
        Identity = identity;
    }

    public async ValueTask<Unit> Handle(ReindexCommand request, CancellationToken cancellationToken)
    {
        await Search.EnsureIndexSettingsAsync(cancellationToken);

        var workspaceSlug = Identity.GetWorkspaceKey();

        await IndexTasksAsync(workspaceSlug, cancellationToken);
        await IndexProjectsAsync(workspaceSlug, cancellationToken);
        await IndexBoardsAsync(workspaceSlug, cancellationToken);
        await IndexSprintsAsync(workspaceSlug, cancellationToken);

        return default;
    }

    private async Task IndexTasksAsync(string workspaceSlug, CancellationToken ct)
    {
        var tasks = await UnitOfWork.Tasks.GetTasksAsync(workspaceSlug, cancellationToken: ct);

        var documents = tasks.Select(t => new TaskSearchDocument
        {
            Id = $"task_{t.Id}",
            TaskId = t.Id,
            Title = t.Name,
            Description = t.Description,
            SystemId = t.SystemId,
            WorkspaceSlug = workspaceSlug,
            Status = t.Status.ToString(),
            Priority = t.Priority?.ToString(),
            AssigneeIds = t.Assignees.Select(a => a.Id).ToList(),
            ProjectId = t.ProjectId,
            UpdatedAt = DateTime.UtcNow,
        });

        foreach (var batch in documents.Chunk(BatchSize))
        {
            await Search.IndexDocumentsAsync("tasks", batch, ct);
        }
    }

    private async Task IndexProjectsAsync(string workspaceSlug, CancellationToken ct)
    {
        var projects = await UnitOfWork.Projects.GetProjects(workspaceSlug, ct);

        var documents = projects.Select(p => new ProjectSearchDocument
        {
            Id = $"project_{p.Id}",
            ProjectId = p.Id,
            Name = p.Name,
            Description = p.Description,
            Key = p.Key,
            WorkspaceSlug = workspaceSlug,
            UpdatedAt = p.UpdatedAt ?? p.CreatedAt,
        });

        foreach (var batch in documents.Chunk(BatchSize))
        {
            await Search.IndexDocumentsAsync("projects", batch, ct);
        }
    }

    private async Task IndexBoardsAsync(string workspaceSlug, CancellationToken ct)
    {
        var boards = await UnitOfWork.Boards.GetBoards(workspaceSlug, isReadonly: true, ct);

        var documents = boards.Select(b => new BoardSearchDocument
        {
            Id = $"board_{b.Id}",
            BoardId = b.Id,
            Name = b.Name,
            WorkspaceSlug = workspaceSlug,
            ProjectId = b.ProjectId,
            Identifier = b.Identifier,
            UpdatedAt = b.UpdatedAt ?? b.CreatedAt,
        });

        foreach (var batch in documents.Chunk(BatchSize))
        {
            await Search.IndexDocumentsAsync("boards", batch, ct);
        }
    }

    private async Task IndexSprintsAsync(string workspaceSlug, CancellationToken ct)
    {
        var sprints = await UnitOfWork.Sprints.GetSprintsAsync(workspaceSlug, cancellationToken: ct);

        var documents = sprints.Select(s => new SprintSearchDocument
        {
            Id = $"sprint_{s.Id}",
            SprintId = s.Id,
            Name = s.Name,
            Goal = s.Goal,
            WorkspaceSlug = workspaceSlug,
            ProjectId = s.ProjectId,
            Status = s.Status.ToString(),
            UpdatedAt = s.UpdatedAt ?? s.CreatedAt,
        });

        foreach (var batch in documents.Chunk(BatchSize))
        {
            await Search.IndexDocumentsAsync("sprints", batch, ct);
        }
    }
}
