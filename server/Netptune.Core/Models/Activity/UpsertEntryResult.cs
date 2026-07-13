using Netptune.Core.Entities;

namespace Netptune.Core.Models.Activity;

public abstract record UpsertEntryResult
{
    public sealed record Upserted(ActivityEntry Entry) : UpsertEntryResult;

    public sealed record SlotHeldByStaleEntry : UpsertEntryResult;
}
