using Netptune.Core.Enums;
using Netptune.Core.Services.Ai;

namespace Netptune.Services.Ai;

public sealed class AiExecutionContext : IAiExecutionContext
{
    public bool IsActive { get; private set; }

    public string? Agent { get; private set; }

    public Guid? CorrelationId { get; private set; }

    public EventOriginType OriginType => EventOriginType.Assistant;

    public IDisposable Begin(string agent, Guid correlationId)
    {
        IsActive = true;
        Agent = agent;
        CorrelationId = correlationId;

        return new Scope(this);
    }

    private void End()
    {
        IsActive = false;
        Agent = null;
        CorrelationId = null;
    }

    private sealed class Scope : IDisposable
    {
        private readonly AiExecutionContext Context;

        public Scope(AiExecutionContext context)
        {
            Context = context;
        }

        public void Dispose()
        {
            Context.End();
        }
    }
}
