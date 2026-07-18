using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Netptune.Core.Entities;

namespace Netptune.Entities.Interceptors;

// Guards EventRecord only. ActivityEntry, the feed projection, is mutated in place on every merge and must
// never be added here.
public sealed class EventRecordImmutabilityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ThrowIfEventRecordMutated(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfEventRecordMutated(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ThrowIfEventRecordMutated(DbContext? context)
    {

        if (context is null)
        {
            return;
        }

        var mutated = context.ChangeTracker
            .Entries<EventRecord>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (mutated)
        {
            throw new InvalidOperationException("EventRecord records are immutable and cannot be updated or deleted.");
        }
    }
}
