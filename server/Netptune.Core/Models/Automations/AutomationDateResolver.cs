using Netptune.Core.Enums;

namespace Netptune.Core.Models.Automations;

public static class AutomationDateResolver
{
    public static DateOnly? Resolve(AutomationDateUpdate update, DateOnly today)
    {
        return update.Mode switch
        {
            AutomationDateUpdateMode.Absolute => update.Date,
            AutomationDateUpdateMode.RelativeDays => today.AddDays(update.Offset ?? 0),
            AutomationDateUpdateMode.RelativeBusinessDays => AddBusinessDays(today, update.Offset ?? 0),
            _ => null,
        };
    }

    public static DateOnly? ResolveOrKeep(DateOnly? currentDate, AutomationDateUpdate? update, DateOnly today)
    {
        if (update is null)
        {
            return currentDate;
        }

        return Resolve(update, today);
    }

    private static DateOnly AddBusinessDays(DateOnly date, int offset)
    {
        var remaining = Math.Abs(offset);
        var direction = Math.Sign(offset);
        var result = date;

        while (remaining > 0)
        {
            result = result.AddDays(direction);
            var isWeekday = result.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday;

            if (isWeekday)
            {
                remaining--;
            }
        }

        return result;
    }
}
