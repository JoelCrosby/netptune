using Netptune.Core.Entities;
using Netptune.Core.Events;

namespace Netptune.Core.Services;

public interface IEventRecordWriter
{
    Task<EventRecord> Append<TPayload>(
        EventWriteRequest<TPayload> request,
        CancellationToken cancellationToken = default)
        where TPayload : class;
}
