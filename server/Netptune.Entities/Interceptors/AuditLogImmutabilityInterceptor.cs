using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

using Netptune.Core.Entities;

namespace Netptune.Entities.Interceptors;

public sealed class AuditLogImmutabilityInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ThrowIfAuditLogMutated(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        ThrowIfAuditLogMutated(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ThrowIfAuditLogMutated(DbContext? context)
    {
        if (context is null) return;

        var mutated = context.ChangeTracker
            .Entries<ActivityLog>()
            .Any(e => e.State is EntityState.Modified or EntityState.Deleted);

        if (mutated)
        {
            throw new InvalidOperationException("ActivityLog records are immutable and cannot be updated or deleted.");
        }
    }
}
