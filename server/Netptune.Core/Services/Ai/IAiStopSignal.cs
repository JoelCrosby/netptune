namespace Netptune.Core.Services.Ai;

// Carries a stop to every app instance, since the run it is meant for may be on another one.
public interface IAiStopSignal
{
    Task Publish(Guid operationId);

    void Subscribe(Action<Guid> onStop);
}
