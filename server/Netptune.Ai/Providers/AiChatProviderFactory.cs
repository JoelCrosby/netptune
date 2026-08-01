using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Ai.Providers;

public sealed class AiChatProviderFactory : IAiChatProviderFactory
{
    private readonly Dictionary<AiProvider, IAiChatProvider> ProvidersByKind;

    public AiChatProviderFactory(IEnumerable<IAiChatProvider> providers)
    {
        ProvidersByKind = providers.ToDictionary(provider => provider.Provider);
    }

    public IAiChatProvider Resolve(AiProvider provider)
    {
        var found = ProvidersByKind.TryGetValue(provider, out var resolved);

        if (!found)
        {
            throw new InvalidOperationException($"No AI chat provider registered for {provider}.");
        }

        return resolved!;
    }
}
