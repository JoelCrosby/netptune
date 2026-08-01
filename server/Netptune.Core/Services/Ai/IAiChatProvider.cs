using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;

namespace Netptune.Core.Services.Ai;

public sealed record AiProviderStreamEvent
{
    public string? TextDelta { get; init; }

    public AiChatTurn? CompletedTurn { get; init; }

    public static AiProviderStreamEvent Delta(string text)
    {
        return new AiProviderStreamEvent { TextDelta = text };
    }

    public static AiProviderStreamEvent Completed(AiChatTurn turn)
    {
        return new AiProviderStreamEvent { CompletedTurn = turn };
    }
}

public interface IAiChatProvider
{
    AiProvider Provider { get; }

    string DefaultModel { get; }

    IAsyncEnumerable<AiProviderStreamEvent> Stream(AiChatRequest request, string apiKey, CancellationToken cancellationToken);
}

public interface IAiChatProviderFactory
{
    IAiChatProvider Resolve(AiProvider provider);
}
