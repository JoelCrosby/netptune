using Mediator;

using Netptune.Core.Services;
using Netptune.Core.Services.Search;
using Netptune.Core.UnitOfWork;
using Netptune.Core.ViewModels.ProjectTasks;
using Netptune.Core.ViewModels.Search;

namespace Netptune.Handlers.Search;

public sealed record SearchQuery(string Q, string[]? Types = null, int Limit = 20) : IRequest<SearchResponse>;

public sealed class SearchQueryHandler : IRequestHandler<SearchQuery, SearchResponse>
{
    private readonly IMeilisearchService Search;
    private readonly IIdentityService Identity;
    private readonly INetptuneUnitOfWork UnitOfWork;

    public SearchQueryHandler(IMeilisearchService search, IIdentityService identity, INetptuneUnitOfWork unitOfWork)
    {
        Search = search;
        Identity = identity;
        UnitOfWork = unitOfWork;
    }

    public async ValueTask<SearchResponse> Handle(SearchQuery request, CancellationToken cancellationToken)
    {
        var workspaceSlug = Identity.GetWorkspaceKey();
        var query = new GlobalSearchQuery(request.Q, workspaceSlug, request.Types, request.Limit);
        var response = await Search.SearchAsync(query, cancellationToken);
        var results = await HydrateTaskResults(response.Results, workspaceSlug, cancellationToken);
        var exactTask = await ResolveExactTask(request, workspaceSlug, cancellationToken);

        if (exactTask is not null)
        {
            results.RemoveAll(result => result.Type == "task" && result.Id == exactTask.Id);
            results.Insert(0, ToSearchResult(exactTask, workspaceSlug));
        }

        return response with { Results = results };
    }

    private async Task<List<SearchResultViewModel>> HydrateTaskResults(
        IReadOnlyCollection<SearchResultViewModel> results,
        string workspaceSlug,
        CancellationToken cancellationToken)
    {
        var taskIds = results
            .Where(result => result.Type == "task")
            .Select(result => result.Id)
            .Distinct()
            .ToList();

        if (taskIds.Count == 0)
        {
            return [.. results];
        }

        var references = await UnitOfWork.Tasks.GetTaskSearchReferences(taskIds, workspaceSlug, cancellationToken);
        var referencesByTaskId = references.ToDictionary(reference => reference.TaskId);
        var hydratedResults = new List<SearchResultViewModel>(results.Count);

        foreach (var result in results)
        {
            if (result.Type != "task")
            {
                hydratedResults.Add(result);
                continue;
            }

            if (!referencesByTaskId.TryGetValue(result.Id, out var reference))
            {
                continue;
            }

            var metadata = new Dictionary<string, object?>(result.Metadata)
            {
                ["projectKey"] = reference.ProjectKey,
            };
            var status = metadata.GetValueOrDefault("status")?.ToString() ?? string.Empty;

            hydratedResults.Add(result with
            {
                Subtitle = BuildTaskSubtitle(reference.ProjectKey, status),
                Url = $"/{workspaceSlug}/tasks/{reference.SystemId}",
                Metadata = metadata,
            });
        }

        return hydratedResults;
    }

    private async Task<TaskViewModel?> ResolveExactTask(
        SearchQuery request,
        string workspaceSlug,
        CancellationToken cancellationToken)
    {
        var includesTasks = request.Types is null || request.Types.Any(IsTaskType);
        var query = request.Q.Trim();
        var separatorIndex = query.LastIndexOf('-');
        var hasIdentifierShape = separatorIndex > 0
            && separatorIndex < query.Length - 1
            && int.TryParse(query[(separatorIndex + 1)..], out _);
        var shouldResolveTask = includesTasks && hasIdentifierShape;

        if (!shouldResolveTask)
        {
            return null;
        }

        return await UnitOfWork.Tasks.GetTaskViewModel(query, workspaceSlug, cancellationToken);
    }

    private static bool IsTaskType(string type)
    {
        return string.Equals(type, "task", StringComparison.OrdinalIgnoreCase)
            || string.Equals(type, "tasks", StringComparison.OrdinalIgnoreCase);
    }

    private static SearchResultViewModel ToSearchResult(TaskViewModel task, string workspaceSlug)
    {
        var projectKey = GetProjectKey(task);

        return new SearchResultViewModel
        {
            Type = "task",
            Id = task.Id,
            Title = task.Name,
            Subtitle = BuildTaskSubtitle(projectKey, task.StatusName),
            Url = $"/{workspaceSlug}/tasks/{task.SystemId}",
            Metadata = new Dictionary<string, object?>
            {
                ["status"] = task.StatusName,
                ["priority"] = task.Priority?.ToString(),
                ["projectId"] = task.ProjectId,
                ["projectScopeId"] = task.ProjectScopeId,
                ["projectKey"] = projectKey,
            },
        };
    }

    private static string? GetProjectKey(TaskViewModel task)
    {
        if (!task.ProjectId.HasValue)
        {
            return null;
        }

        var suffix = $"-{task.ProjectScopeId}";

        return task.SystemId.EndsWith(suffix, StringComparison.Ordinal)
            ? task.SystemId[..^suffix.Length]
            : null;
    }

    private static string BuildTaskSubtitle(string? projectKey, string status)
    {
        return string.IsNullOrWhiteSpace(projectKey) ? status : $"{projectKey} · {status}";
    }
}
