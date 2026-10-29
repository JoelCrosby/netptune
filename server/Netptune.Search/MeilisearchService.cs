using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

using Meilisearch;

using Microsoft.Extensions.Logging;

using Netptune.Core.Models.Search;
using Netptune.Core.Services.Search;
using Netptune.Core.ViewModels.Search;

using MeiliSearchQuery = Meilisearch.SearchQuery;

namespace Netptune.Search;

public sealed class MeilisearchService : IMeilisearchService
{
    private const int PageSnippetWords = 24;

    private static readonly string[] AllIndexes = ["tasks", "projects", "boards", "sprints", PageSearchDocuments.IndexName];

    private readonly MeilisearchClient Client;
    private readonly ILogger<MeilisearchService> Logger;

    public MeilisearchService(MeilisearchClient client, ILogger<MeilisearchService> logger)
    {
        Client = client;
        Logger = logger;
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
        var federatedQuery = new FederatedMultiSearchQuery
        {
            Queries = indexes.Select(index => BuildIndexQuery(index, query.Q, filter)).ToList(),
            FederationOptions = new MultiSearchFederationOptions { Limit = query.Limit },
        };

        var hits = await FederatedSearchAsync(federatedQuery, cancellationToken);
        var results = hits.Select(ToSearchResult).OfType<SearchResultViewModel>().ToList();

        sw.Stop();

        return new SearchResponse
        {
            Results = results,
            ProcessingTimeMs = sw.ElapsedMilliseconds,
        };
    }

    public async Task<TaskIdSearchResult?> SearchTaskIdsAsync(TaskIdSearchQuery query, CancellationToken cancellationToken = default)
    {
        var filters = new List<string> { $"workspaceSlug = \"{EscapeFilterValue(query.WorkspaceSlug)}\"" };

        if (query.ProjectId.HasValue)
        {
            filters.Add($"projectId = {query.ProjectId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(query.AssigneeId))
        {
            filters.Add($"assigneeIds = \"{EscapeFilterValue(query.AssigneeId)}\"");
        }

        var meiliQuery = new MeiliSearchQuery
        {
            Filter = string.Join(" AND ", filters),
            Limit = query.Limit,
            AttributesToRetrieve = ["taskId"],
        };

        try
        {
            var result = await Client.Index("tasks").SearchAsync<TaskIdHit>(query.Q, meiliQuery, cancellationToken);
            var taskIds = result.Hits.Select(hit => hit.TaskId).ToList();
            var estimatedTotalHits = result is SearchResult<TaskIdHit> searchResult ? searchResult.EstimatedTotalHits : taskIds.Count;

            return new TaskIdSearchResult(taskIds, estimatedTotalHits);
        }
        catch (Exception exception) when (exception is MeilisearchApiError or MeilisearchCommunicationError or HttpRequestException)
        {
            return null;
        }
    }

    private static string EscapeFilterValue(string value)
    {
        return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private async Task<IReadOnlyCollection<JsonElement>> FederatedSearchAsync(
        FederatedMultiSearchQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            var result = await Client.FederatedMultiSearchAsync<JsonElement>(query, cancellationToken);

            return result.Hits;
        }
        catch (MeilisearchApiError ex) when (ex.Code == "index_not_found")
        {
            Logger.LogWarning(ex, "Search returned no results because a Meilisearch index is missing");

            return [];
        }
    }

    private static FederatedSearchQuery BuildIndexQuery(string indexName, string q, string filter)
    {
        if (indexName != PageSearchDocuments.IndexName)
        {
            return new FederatedSearchQuery { IndexUid = indexName, Q = q, Filter = filter };
        }

        return new FederatedSearchQuery
        {
            IndexUid = indexName,
            Q = q,
            Filter = filter,
            AttributesToRetrieve = ["pageId", "slug", "title", "path", "kind", "projectId", "workspaceSlug"],
            AttributesToCrop = ["body"],
            CropLength = PageSnippetWords,
        };
    }

    private static SearchResultViewModel? ToSearchResult(JsonElement hit)
    {
        var indexName = hit.GetProperty("_federation").GetProperty("indexUid").GetString();

        return indexName switch
        {
            "tasks" => ToTaskResult(ReadHit<TaskSearchDocument>(hit)),
            "projects" => ToProjectResult(ReadHit<ProjectSearchDocument>(hit)),
            "boards" => ToBoardResult(ReadHit<BoardSearchDocument>(hit)),
            "sprints" => ToSprintResult(ReadHit<SprintSearchDocument>(hit)),
            PageSearchDocuments.IndexName => ToPageResult(ReadHit<PageSearchHit>(hit)),
            _ => null,
        };
    }

    private static T ReadHit<T>(JsonElement hit)
    {
        return hit.Deserialize<T>(JsonSerializerOptions.Web)!;
    }

    private static SearchResultViewModel ToTaskResult(TaskSearchDocument hit)
    {
        return new SearchResultViewModel
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
        };
    }

    private static SearchResultViewModel ToProjectResult(ProjectSearchDocument hit)
    {
        return new SearchResultViewModel
        {
            Type = "project",
            Id = hit.ProjectId,
            Title = hit.Name,
            Subtitle = hit.Key,
            Url = $"/{hit.WorkspaceSlug}/projects/{hit.ProjectId}",
            Metadata = new Dictionary<string, object?> { ["key"] = hit.Key },
        };
    }

    private static SearchResultViewModel ToBoardResult(BoardSearchDocument hit)
    {
        return new SearchResultViewModel
        {
            Type = "board",
            Id = hit.BoardId,
            Title = hit.Name,
            Subtitle = "Board",
            Url = $"/{hit.WorkspaceSlug}/boards/{hit.Identifier}",
            Metadata = new Dictionary<string, object?> { ["projectId"] = hit.ProjectId },
        };
    }

    private static SearchResultViewModel ToSprintResult(SprintSearchDocument hit)
    {
        return new SearchResultViewModel
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
        };
    }

    private static SearchResultViewModel ToPageResult(PageSearchHit hit)
    {
        return new SearchResultViewModel
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
        };
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

    private sealed record TaskIdHit
    {
        public int TaskId { get; init; }
    }

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
