using System.Diagnostics;
using System.Text.Json.Serialization;

using Meilisearch;

using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;
using Netptune.Core.ViewModels.Search;

using MeiliSearchQuery = Meilisearch.SearchQuery;

namespace Netptune.Search;

public sealed class MeilisearchService : IMeilisearchService
{
    // Words of body around the match that a page result carries, enough to tell two hits apart.
    private const int PageSnippetWords = 24;

    private static readonly string[] AllIndexes = ["tasks", "projects", "boards", "sprints", PageSearchDocuments.IndexName];

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

    public async Task DeleteDocumentsAsync(string indexName, IEnumerable<string> ids, CancellationToken cancellationToken = default)
    {
        var documentIds = ids.ToList();

        if (documentIds.Count == 0) return;

        var index = Client.Index(indexName);
        await index.DeleteDocumentsAsync(documentIds, cancellationToken);
    }

    public async Task<bool> IsIndexEmptyAsync(string indexName, CancellationToken cancellationToken = default)
    {
        try
        {
            var stats = await Client.Index(indexName).GetStatsAsync(cancellationToken);

            return stats.NumberOfDocuments == 0;
        }
        catch (MeilisearchApiError error) when (error.Code == "index_not_found")
        {
            return true;
        }
    }

    public async Task EnsureIndexSettingsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureIndexExistsAsync("tasks", cancellationToken);
        await EnsureIndexExistsAsync("projects", cancellationToken);
        await EnsureIndexExistsAsync("boards", cancellationToken);
        await EnsureIndexExistsAsync("sprints", cancellationToken);
        await EnsureIndexExistsAsync(PageSearchDocuments.IndexName, cancellationToken);

        await EnsureTasksIndexAsync(cancellationToken);
        await EnsureProjectsIndexAsync(cancellationToken);
        await EnsureBoardsIndexAsync(cancellationToken);
        await EnsureSprintsIndexAsync(cancellationToken);
        await EnsurePagesIndexAsync(cancellationToken);
    }

    private async Task EnsureIndexExistsAsync(string indexName, CancellationToken ct)
    {
        try
        {
            var creation = await Client.CreateIndexAsync(indexName, "id", ct);

            await WaitForCompletionAsync(creation, ct);
        }
        catch (MeilisearchApiError ex) when (ex.Code == "index_already_exists")
        {
        }
    }

    private Task WaitForCompletionAsync(TaskInfo task, CancellationToken ct)
    {
        return Client.WaitForTaskAsync(task.TaskUid, cancellationToken: ct);
    }

    public async Task<SearchResponse> SearchAsync(GlobalSearchQuery query, CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();

        var indexes = ResolveIndexes(query.Types, query.IncludePages);
        var filter = $"workspaceSlug = \"{EscapeFilterValue(query.WorkspaceSlug)}\"";
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

    private static string EscapeFilterValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
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
                PageSearchDocuments.IndexName => await SearchPagesAsync(q, meiliQuery, cancellationToken),
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
            Subtitle = hit.Status,
            Url = string.Empty,
            Metadata = new Dictionary<string, object?>
            {
                ["status"] = hit.Status,
                ["priority"] = hit.Priority,
                ["projectId"] = hit.ProjectId,
                ["projectScopeId"] = hit.ProjectScopeId,
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
            Url = $"/{hit.WorkspaceSlug}/sprints/{hit.Identifier}",
            Metadata = new Dictionary<string, object?>
            {
                ["status"] = hit.Status,
                ["projectId"] = hit.ProjectId,
            },
        });
    }

    private async Task<IEnumerable<SearchResultViewModel>> SearchPagesAsync(string q, MeiliSearchQuery query, CancellationToken cancellationToken)
    {
        var index = Client.Index(PageSearchDocuments.IndexName);
        var pageQuery = new MeiliSearchQuery
        {
            Filter = query.Filter,
            Limit = query.Limit,
            AttributesToRetrieve = ["pageId", "slug", "title", "path", "kind", "projectId", "workspaceSlug"],
            AttributesToCrop = ["body"],
            CropLength = PageSnippetWords,
        };
        var result = await index.SearchAsync<PageSearchHit>(q, pageQuery, cancellationToken);

        return result.Hits.Select(hit => new SearchResultViewModel
        {
            Type = "page",
            Id = hit.PageId,
            Title = hit.Title,
            Subtitle = hit.Path ?? "Page",
            Url = $"/{hit.WorkspaceSlug}/pages/{hit.Slug}",
            Metadata = new Dictionary<string, object?>
            {
                ["slug"] = hit.Slug,
                ["kind"] = hit.Kind,
                ["path"] = hit.Path,
                ["projectId"] = hit.ProjectId,
                ["snippet"] = hit.Formatted?.Body,
            },
        });
    }

    private static IReadOnlyList<string> ResolveIndexes(string[]? types, bool includePages)
    {
        var requested = types is { Length: > 0 }
            ? types.Select(t => t.ToLowerInvariant()).Where(t => AllIndexes.Contains(t))
            : AllIndexes;

        return requested
            .Where(index => includePages || index != PageSearchDocuments.IndexName)
            .Distinct()
            .ToList();
    }

    private async Task EnsureTasksIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("tasks");
        var update = await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["title", "description"],
            FilterableAttributes = ["workspaceSlug", "status", "priority", "assigneeIds", "tagIds", "projectId"],
            SortableAttributes = ["updatedAt"],
        }, ct);

        await WaitForCompletionAsync(update, ct);
    }

    private async Task EnsureProjectsIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("projects");
        var update = await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["name", "description", "key"],
            FilterableAttributes = ["workspaceSlug"],
            SortableAttributes = ["updatedAt"],
        }, ct);

        await WaitForCompletionAsync(update, ct);
    }

    private async Task EnsureBoardsIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("boards");
        var update = await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["name"],
            FilterableAttributes = ["workspaceSlug", "projectId"],
            SortableAttributes = ["updatedAt"],
        }, ct);

        await WaitForCompletionAsync(update, ct);
    }

    private async Task EnsureSprintsIndexAsync(CancellationToken ct)
    {
        var index = Client.Index("sprints");
        var update = await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["name", "goal"],
            FilterableAttributes = ["workspaceSlug", "projectId", "status"],
            SortableAttributes = ["updatedAt"],
        }, ct);

        await WaitForCompletionAsync(update, ct);
    }

    private async Task EnsurePagesIndexAsync(CancellationToken ct)
    {
        var index = Client.Index(PageSearchDocuments.IndexName);
        var update = await index.UpdateSettingsAsync(new Settings
        {
            SearchableAttributes = ["title", "tags", "path", "body"],
            FilterableAttributes = ["workspaceSlug", "projectId", "kind", "status"],
            SortableAttributes = ["updatedAt"],
        }, ct);

        await WaitForCompletionAsync(update, ct);
    }

    // The body is cropped around the match rather than returned whole, which keeps a result list small.
    private sealed record PageSearchHit
    {
        public int PageId { get; init; }

        public string Slug { get; init; } = string.Empty;

        public string Title { get; init; } = string.Empty;

        public string? Path { get; init; }

        public string Kind { get; init; } = string.Empty;

        public int? ProjectId { get; init; }

        public string WorkspaceSlug { get; init; } = string.Empty;

        [JsonPropertyName("_formatted")]
        public PageSearchSnippet? Formatted { get; init; }
    }

    private sealed record PageSearchSnippet
    {
        public string? Body { get; init; }
    }
}
