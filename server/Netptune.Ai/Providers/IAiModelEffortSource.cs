using Netptune.Core.Enums;

namespace Netptune.Ai.Providers;

public interface IAiModelEffortSource
{
    AiProvider Provider { get; }

    Task<IReadOnlyDictionary<string, IReadOnlyList<AiEffort>>> GetEfforts(string apiKey, CancellationToken cancellationToken);
}
