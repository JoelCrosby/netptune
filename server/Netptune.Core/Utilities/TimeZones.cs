namespace Netptune.Core.Utilities;

public static class TimeZones
{
    public const string Default = "UTC";

    public static bool IsValid(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return false;
        }

        return TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out _);
    }

    public static TimeZoneInfo Find(string? timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
        {
            return TimeZoneInfo.Utc;
        }

        TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone);

        return timeZone ?? TimeZoneInfo.Utc;
    }
}
