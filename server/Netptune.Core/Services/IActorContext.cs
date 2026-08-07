namespace Netptune.Core.Services;

public sealed record ActorIdentity(string UserId, int WorkspaceId, string WorkspaceKey);

public interface IActorContext
{
    ActorIdentity? Current { get; }

    IDisposable Begin(ActorIdentity actor);
}
