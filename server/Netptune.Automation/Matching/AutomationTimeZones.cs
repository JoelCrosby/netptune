using Netptune.Core.Entities;
using Netptune.Core.Utilities;

namespace Netptune.Automation.Matching;

internal static class AutomationTimeZones
{
    public static DateOnly Today(AutomationRule rule, DateTime utcNow)
    {
        var timeZone = TimeZones.Find(rule.Workspace.MetaInfo?.TimeZone);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utcNow, DateTimeKind.Utc),
            timeZone);

        return DateOnly.FromDateTime(localTime);
    }
}
