using Netptune.Core.Services;

namespace Netptune.Services.Activity;

public sealed class ActorContext : IActorContext
{
    public ActorIdentity? Current { get; private set; }

    public IDisposable Begin(ActorIdentity actor)
    {
        Current = actor;

        return new Scope(this);
    }

    private void End()
    {
        Current = null;
    }

    private sealed class Scope : IDisposable
    {
        private readonly ActorContext Context;

        public Scope(ActorContext context)
        {
            Context = context;
        }

        public void Dispose()
        {
            Context.End();
        }
    }
}
