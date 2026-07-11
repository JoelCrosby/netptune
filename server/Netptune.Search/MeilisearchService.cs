using System.Diagnostics;

using Meilisearch;

using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;
using Netptune.Core.ViewModels.Search;

using MeiliSearchQuery = Meilisearch.SearchQuery;

namespace Netptune.Search;

public sealed class MeilisearchService : IMeilisearchService
{
    private static readonly string[] AllIndexes = ["tasks", "projects", "boards", "sprints"];

    private readonly MeilisearchClient Client;

    public MeilisearchService(MeilisearchClient client)
    {
        Client = client;
    }

    public async Task IndexDocumentsAsync<T>(string indexName, IEnumerable<T> docs, CancellationToken cancellationToken = default)
    {
        var index = Client.Index(indexName);
        await index.AddDocumentsAsync(docs, cancellationToken: cancellationToken);
    }

    public async Task DeleteDocumentAsync(string indexName, string id, CancellationToken cancellationToken = default)
    {
        var index = Client.Index(indexName);
        await index.DeleteOneDocumentAsync(id, cancellationToken);
    }

    public async Task DeleteDocumentsAsync(string indexName, IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var documentIds = ids.ToList();

        if (documentIds.Count == 0) return;

        var index = Client.Index(indexName);
        await index.DeleteDocumentsAsync(documentIds, cancellationToken);
    }

    public async Task EnsureIndexSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureIndexExistsAsync("tasks", cancellationToken);
        await EnsureIndexExistsAsync("projects", cancellationToken);
        await EnsureIndexExistsAsync("boards", cancellationToken);
        await EnsureIndexExistsAsync("sprints", cancellationToken);

        await EnsureTasksIndexAsync(cancellationToken);
        await EnsureProjectsIndexAsync(cancellationToken);
        await EnsureBoardsIndexAsync(cancellationToken);
        await EnsureSprintsIndexAsync(cancellationToken);
    }

    private async Task EnsureIndexExistsAsync(string indexName, CancellationToken ct)
    {
        try
        {
            await Client.CreateIndexAsync(indexName, "id", ct);
        }
        catch (MeilisearchApiError ex) when (ex.Code == "index_already_exists")
        {
        }
    }

    public async Task<SearchResponse> SearchAsync(GlobalSearchQuery query, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var indexes = ResolveIndexes(query.Types);
        var filter = $"workspaceSlug = \"{query.WorkspaceSlug}\"";
        var meiliQuery = new MeiliSearchQuery { Filter = filter, Limit = query.Limit };

        var tasks = indexes.Select(idx => SearchIndexAsync(idx, query.Q, meiliQuery, cancellationToken));
        var resultSets = await Task.WhenAll(tasks);

        sw.Stop();

        return new SearchResponse
        {
            Results = resultSets.SelectMany(r => r).ToList(),
            ProcessingTimeMs = sw.ElapsedMilliseconds,
        };
    }

    private async Task<IEnumerable<SearchResultViewModel>> SearchIndexAsync(string indexName, string q, MeiliSearchQuery meiliQuery, CancellationToken cancellationToken)
    {
        try
        {
            return indexName switch
            {
                "tasks" => await SearchTasksAsync(q, meiliQuery, cancellationToken),
                "projects" => await SearchProjectsAsync(q, meiliQuery, cancellationToken),
                "boards" => await SearchBoardsAsync(q, meiliQuery, cancellationToken),
                "sprints" => await SearchSprintsAsync(q, meiliQuery, cancellationToken),
                _ => [],
            };
        }
        catch (MeilisearchApiError ex) when (ex.Code == "index_not_found")
        {
            return [];
        }
    }

    private async Task<IEnumerable<SearchResultViewModel>> SearchTasksAsync(string q, MeiliSearchQuery query, CancellationToken cancellationToken)
    {
        var index = Client.Index("tasks");
        var result = await index.SearchAsync<TaskSearchDocument>(q, query, cancellationToken);

        return result.Hits.Select(hit => new SearchResultViewModel
        {
            Type = "task",
            Id = hit.TaskId,
            Title = hit.Title,
            Subtitle = $"{hit.ProjectKey} · {hit.Status}",
            Url = $"/{hit.WorkspaceSlug}/tasks/{hit.SystemId}",
            Metadata = new Dictionary<string, object?>
            {
                ["status"] = hit.Status,
                ["priority"] = hit.Priority,
                ["projectKey"] = hit.ProjectKey,
            },
        });
    }

    private async Task<IEnumerable<SearchResultViewModel>> SearchProjectsAsync(string q, MeiliSearchQuery query, CancellationToken cancellationToken)
    {
        var index = Client.Index("projects");
        var result = await index.SearchAsync<ProjectSearchDocument>(q, query, cancellationToken);

        return result.Hits.Select(hit => new SearchResultViewModel
        {
            Type = "project",
            Id = hit.ProjectId,
            Title = hit.Name,
            Subtitle = hit.Key,
            Url = $"/{hit.WorkspaceSlug}/projects/{hit.ProjectId}",
            Metadata = new Dictionary<string, object?> { ["key"] = hit.Key },
        });
    }

    private async Task<IEnumerable<SearchResultViewModel>> SearchBoardsAsync(string q, MeiliSearchQuery query, CancellationToken cancellationToken)
    {
        var index = Client.Index("boards");
        var result = await index.SearchAsync<BoardSearchDocument>(q, query, cancellationToken);

        return result.Hits.Select(hit => new SearchResultViewModel
        {
            Type = "board",
            Id = hit.BoardId,
            Title = hit.Name,
            Subtitle = "Board",
            Url = $"/{hit.WorkspaceSlug}/boards/{hit.Identifier}",
            Metadata = new Dictionary<string, object?> { ["projectId"] = hit.ProjectId },
        });
    }

    private async Task<IEnumerable<SearchResultViewModel>> SearchSprintsAsync(string q, MeiliSearchQuery query, CancellationToken cancellationToken)
    {
        var index = Client.Index("sprints");
        var result = await index.SearchAsync<SprintSearchDocument>(q, query, cancellationToken);

        return result.Hits.Select(hit => new SearchResultViewModel
        {
            Type = "sprint",
            Id = hit.SprintId,
            Title = hit.Name,
            Subtitle = hit.Status,
            Url = $"/{hit.WorkspaceSlug}/sprints/{hit.SprintId}",
            Metadata = new Dictionary<string, object?>
            {
                ["status"] = hit.Status,
                ["projectId"] = hit.ProjectId,
            },
        });
    }

    private static IReadOnlyList<string> ResolveIndexes(string[]? types)
    {
        if (types is not { Length: > 0 }) return AllIndexes;

        return types
            .Select(t => t.ToLowerInvariant())
            .Where(t => AllIndexes.Contains(t))
            .ToList();
    }

    private async Task EnsureTasksIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("tasks");
        await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["title", "description", "systemId"],
            FilterableAttributes = ["workspaceSlug", "status", "priority", "assigneeIds", "tagIds", "projectId"],
            SortableAttributes = ["updatedAt"],
        }, ct);
    }

    private async Task EnsureProjectsIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("projects");
        await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["name", "description", "key"],
            FilterableAttributes = ["workspaceSlug"],
            SortableAttributes = ["updatedAt"],
        }, ct);
    }

    private async Task EnsureBoardsIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("boards");
        await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["name"],
            FilterableAttributes = ["workspaceSlug", "projectId"],
            SortableAttributes = ["updatedAt"],
        }, ct);
    }

    private async Task EnsureSprintsIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("sprints");
        await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["name", "goal"],
            FilterableAttributes = ["workspaceSlug", "projectId", "status"],
            SortableAttributes = ["updatedAt"],
        }, ct);
    }
}
