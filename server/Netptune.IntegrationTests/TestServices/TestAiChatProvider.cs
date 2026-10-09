using System.Collections.Concurrent;

using Netptune.Core.Enums;
using Netptune.Core.Models.Ai;
using Netptune.Core.Services.Ai;

namespace Netptune.IntegrationTests.TestServices;

public sealed class TestAiChatScript
{
    private readonly ConcurrentQueue<AiChatTurn> Turns = new();
    private readonly ConcurrentQueue<AiChatRequest> SentRequests = new();

    public TimeSpan DelayBeforeCompletion { get; set; } = TimeSpan.Zero;

    public void Enqueue(AiChatTurn turn)
    {
        Turns.Enqueue(turn);
    }

    public IReadOnlyList<AiChatRequest> Requests => SentRequests.ToList();

    // The runner keeps appending to the list it sends, so keep the messages as they were at the call.
    public void Record(AiChatRequest request)
    {
        SentRequests.Enqueue(request with { Messages = request.Messages.ToList() });
    }

    public void Reset()
    {
        Turns.Clear();
        SentRequests.Clear();
        DelayBeforeCompletion = TimeSpan.Zero;
    }

    public AiChatTurn Next()
    {
        var hasTurn = Turns.TryDequeue(out var turn);

        return hasTurn ? turn! : new AiChatTurn { Text = "Done." };
    }
}

public sealed class TestAiChatProvider : IAiChatProvider
{
    private readonly TestAiChatScript Script;

    public TestAiChatProvider(TestAiChatScript script)
    {
        Script = script;
    }

    public AiProvider Provider => AiProvider.Anthropic;

    public string DefaultModel => "claude-opus-5";

    public async IAsyncEnumerable<AiProviderStreamEvent> Stream(
        AiChatRequest request,
        string apiKey,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Script.Record(request);

        var turn = Script.Next();

        if (!string.IsNullOrEmpty(turn.Text))
        {
            yield return AiProviderStreamEvent.Delta(turn.Text);
        }

        var isDelayed = Script.DelayBeforeCompletion > TimeSpan.Zero;

        if (isDelayed)
        {
            await Task.Delay(Script.DelayBeforeCompletion, cancellationToken);
        }

        yield return AiProviderStreamEvent.Completed(turn);
    }
}

public sealed class TestAiChatProviderFactory : IAiChatProviderFactory
{
    private readonly TestAiChatProvider Provider;

    public TestAiChatProviderFactory(TestAiChatProvider provider)
    {
        Provider = provider;
    }

    public IAiChatProvider Resolve(AiProvider provider)
    {
        return Provider;
    }
}
