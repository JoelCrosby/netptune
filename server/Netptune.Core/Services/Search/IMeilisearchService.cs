using Netptune.Core.ViewModels.Search;

namespace Netptune.Core.Services.Search;

public record GlobalSearchQuery(string Q, string WorkspaceSlug, string[]? Types = null, int Limit = 20);

public interface IMeilisearchService
{
    Task IndexDocumentsAsync<T>(string indexName, IEnumerable<T> docs, CancellationToken cancellationToken = default);
    Task DeleteDocumentAsync(string indexName, string id, CancellationToken cancellationToken = default);
    Task EnsureIndexSettingsAsync(CancellationToken cancellationToken = default);
    Task<SearchResponse> SearchAsync(GlobalSearchQuery query, CancellationToken cancellationToken = default);
}
