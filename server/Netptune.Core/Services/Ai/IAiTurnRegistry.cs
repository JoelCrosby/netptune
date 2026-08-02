namespace Netptune.Core.Services.Ai;

public interface IAiTurnRegistry
{
    IDisposable Register(Guid conversationId, CancellationTokenSource source);

    bool Stop(Guid conversationId);
}
