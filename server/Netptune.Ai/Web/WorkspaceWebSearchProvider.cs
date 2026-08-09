using System.Text.Json;

using Microsoft.Extensions.Options;

using Netptune.Ai.Configuration;
using Netptune.Core.Services;
using Netptune.Core.Services.Ai;
using Netptune.Core.UnitOfWork;

namespace Netptune.Ai.Web;

public sealed class WorkspaceWebSearchProvider : IWebSearchProvider
{
    public const string HttpClientName = "netptune.ai.web.search";

    private readonly HttpClient Client;
    private readonly INetptuneUnitOfWork UnitOfWork;
    private readonly IIdentityService Identity;
    private readonly IAiCredentialProtector Protector;
    private readonly IEnumerable<IWebSearchEngine> Engines;
    private readonly AiWebOptions Options;

    public WorkspaceWebSearchProvider(
        HttpClient client,
        INetptuneUnitOfWork unitOfWork,
        IIdentityService identity,
        IAiCredentialProtector protector,
        IEnumerable<IWebSearchEngine> engines,
        IOptions<AiOptions> options)
    {
        Client = client;
        UnitOfWork = unitOfWork;
        Identity = identity;
        Protector = protector;
        Engines = engines;
        Options = options.Value.Web;
    }

    public bool IsConfigured => true;

    public async Task<WebSearchResult> Search(string query, int take, CancellationToken cancellationToken)
    {
        var workspaceId = await Identity.GetWorkspaceId();
        var stored = await UnitOfWork.WorkspaceSearchCredentials.GetForWorkspace(workspaceId, cancellationToken);

        if (stored is null)
        {
            return WebSearchResult.Failed(
                "No web search provider is set up for this workspace. An admin can add one in workspace settings.");
        }

        var engine = Engines.FirstOrDefault(item => item.Provider == stored.Provider);

        if (engine is null)
        {
            return WebSearchResult.Failed($"Search provider {stored.Provider} is not supported.");
        }

        var credential = new WebSearchCredential
        {
            Provider = stored.Provider,
            ApiKey = stored.Secret is null ? null : Protector.Unprotect(stored.Secret),
            EngineId = stored.EngineId,
            Endpoint = stored.Endpoint,
        };

        var invalid = engine.Validate(credential);

        if (invalid is not null)
        {
            return WebSearchResult.Failed(invalid);
        }

        var count = Math.Clamp(take, 1, Options.MaxSearchResults);
        var hits = await Send(engine, credential, query, count, cancellationToken);

        if (hits.IsSuccess)
        {
            stored.LastUsedAt = DateTime.UtcNow;

            await UnitOfWork.CompleteAsync(cancellationToken);
        }

        return hits;
    }

    private async Task<WebSearchResult> Send(
        IWebSearchEngine engine,
        WebSearchCredential credential,
        string query,
        int take,
        CancellationToken cancellationToken)
    {
        using var request = engine.CreateRequest(credential, query, take);
        using var response = await Client.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return WebSearchResult.Failed($"{engine.Provider} search failed with {(int)response.StatusCode}.");
        }

        await using var payload = await response.Content.ReadAsStreamAsync(cancellationToken);

        using var document = await JsonDocument.ParseAsync(payload, cancellationToken: cancellationToken);

        var hits = engine.ReadHits(document.RootElement);

        return new WebSearchResult { IsSuccess = true, Hits = hits };
    }
}
