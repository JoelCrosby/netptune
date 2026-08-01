using Netptune.Core.Enums;

namespace Netptune.Core.Services.Ai;

public interface IAiExecutionContext
{
    bool IsActive { get; }

    string? Agent { get; }

    Guid? CorrelationId { get; }

    EventOriginType OriginType { get; }

    IDisposable Begin(string agent, Guid correlationId);
}
