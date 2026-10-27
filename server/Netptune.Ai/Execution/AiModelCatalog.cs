using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;

using Netptune.Ai.Providers;
using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Execution;

public sealed class AiModelCatalog : IAiModelCatalog
{
    private static readonly TimeSpan FreshFor = TimeSpan.FromHours(6);
    private static readonly TimeSpan RetryFailureAfter = TimeSpan.FromMinutes(5);

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<AiEffort>> NoLiveEfforts =
        new Dictionary<string, IReadOnlyList<AiEffort>>();

    private readonly Dictionary<AiProvider, IAiModelEffortSource> SourcesByProvider;
    private readonly IAiCredentialProtector Protector;
    private readonly ILogger<AiModelCatalog> Logger;
    private readonly ConcurrentDictionary<AiProvider, CachedEfforts> Cache = new();

    public AiModelCatalog(
        IEnumerable<IAiModelEffortSource> sources,
        IAiCredentialProtector protector,
        ILogger<AiModelCatalog> logger)
    {
        SourcesByProvider = sources.ToDictionary(source => source.Provider);
        Protector = protector;
        Logger = logger;
    }

    public async Task<List<AiModelOption>> List(
        IReadOnlyList<AiResolvedCredential> credentials,
        CancellationToken cancellationToken)
    {
        var lookups = credentials.Select(credential => LookupLiveEfforts(credential, cancellationToken));
        var results = await Task.WhenAll(lookups);
        var liveEffortsByProvider = results.ToDictionary(result => result.Provider, result => result.Efforts);

        return AiModels.Catalog
            .Select(model =>
            {
                var liveEfforts = liveEffortsByProvider.GetValueOrDefault(model.Provider, NoLiveEfforts);

                return model with { Efforts = SelectEfforts(liveEfforts, model.Id) };
            })
            .ToList();
    }

    public async Task<IReadOnlyList<AiEffort>> EffortsFor(
        AiResolvedCredential credential,
        string model,
        CancellationToken cancellationToken)
    {
        var liveEfforts = await GetLiveEfforts(credential, cancellationToken);

        return SelectEfforts(liveEfforts, model);
    }

    private static IReadOnlyList<AiEffort> SelectEfforts(
        IReadOnlyDictionary<string, IReadOnlyList<AiEffort>> liveEfforts,
        string model)
    {
        var hasLiveEfforts = liveEfforts.TryGetValue(model, out var efforts);

        return hasLiveEfforts ? efforts! : AiModels.EffortsFor(model);
    }

    private async Task<ProviderEfforts> LookupLiveEfforts(
        AiResolvedCredential credential,
        CancellationToken cancellationToken)
    {
        var efforts = await GetLiveEfforts(credential, cancellationToken);

        return new ProviderEfforts(credential.Provider, efforts);
    }

    private async Task<IReadOnlyDictionary<string, IReadOnlyList<AiEffort>>> GetLiveEfforts(
        AiResolvedCredential credential,
        CancellationToken cancellationToken)
    {
        var hasSource = SourcesByProvider.TryGetValue(credential.Provider, out var source);

        if (!hasSource)
        {
            return NoLiveEfforts;
        }

        Cache.TryGetValue(credential.Provider, out var cached);

        var isFresh = cached is not null && cached.ExpiresAt > DateTime.UtcNow;

        if (isFresh)
        {
            return cached!.Efforts;
        }

        try
        {
            var apiKey = Protector.Unprotect(credential.Secret);
            var efforts = await source!.GetEfforts(apiKey, cancellationToken);

            Cache[credential.Provider] = new CachedEfforts(efforts, DateTime.UtcNow.Add(FreshFor));

            return efforts;
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested)
        {
            Logger.LogWarning(exception, "Could not read model capabilities from {Provider}.", credential.Provider);

            var staleEfforts = cached?.Efforts ?? NoLiveEfforts;

            Cache[credential.Provider] = new CachedEfforts(staleEfforts, DateTime.UtcNow.Add(RetryFailureAfter));

            return staleEfforts;
        }
    }

    private sealed record ProviderEfforts(
        AiProvider Provider,
        IReadOnlyDictionary<string, IReadOnlyList<AiEffort>> Efforts);

    private sealed record CachedEfforts(
        IReadOnlyDictionary<string, IReadOnlyList<AiEffort>> Efforts,
        DateTime ExpiresAt);
}
