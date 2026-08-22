namespace Netptune.Core.Utilities;

public static class WorkspaceTime
{
    public static DateOnly ToLocalDate(DateTime utcValue, TimeZoneInfo timeZone)
    {
        var utc = DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);

        return DateOnly.FromDateTime(localTime);
    }

    public static DateOnly Today(TimeZoneInfo timeZone, DateTime utcNow)
    {
        return ToLocalDate(utcNow, timeZone);
    }

    public static DateTime StartOfDayUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var dayStart = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);

        // Santiago, Sao Paulo, Beirut, Tehran and Havana move their clocks at midnight, so once a year
        // the day starts at 01:00 local and ConvertTimeToUtc throws on 00:00. Walk to the first minute
        // that exists.
        while (timeZone.IsInvalidTime(dayStart))
        {
            dayStart = dayStart.AddMinutes(1);
        }

        return TimeZoneInfo.ConvertTimeToUtc(dayStart, timeZone);
    }
}
