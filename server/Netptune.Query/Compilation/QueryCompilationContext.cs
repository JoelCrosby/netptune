using Netptune.Core.Utilities;

namespace Netptune.Query.Compilation;

public sealed record QueryCompilationContext
{
    public required DateOnly Today { get; init; }

    public required TimeZoneInfo TimeZone { get; init; }

    public static QueryCompilationContext ForWorkspace(string? timeZoneId, DateTime utcNow)
    {
        var timeZone = TimeZones.Find(timeZoneId);

        return new QueryCompilationContext
        {
            Today = WorkspaceTime.Today(timeZone, utcNow),
            TimeZone = timeZone,
        };
    }

    public DateTime ToInstant(DateOnly date)
    {
        return WorkspaceTime.StartOfDayUtc(date, TimeZone);
    }
}
