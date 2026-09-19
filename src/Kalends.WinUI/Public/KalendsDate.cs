namespace Kalends;

/// <summary>Culture-aware date helpers shared by the views and the layout engine.</summary>
public static class KalendsDate
{
    public static DateTime StartOfWeek(DateTime date, DayOfWeek firstDayOfWeek)
    {
        int delta = ((int)date.DayOfWeek - (int)firstDayOfWeek + 7) % 7;
        return date.Date.AddDays(-delta);
    }

    public static DateTime MonthGridStart(DateTime date, DayOfWeek firstDayOfWeek) =>
        StartOfWeek(new DateTime(date.Year, date.Month, 1), firstDayOfWeek);

    public static double Snap(double minutes, int snapMinutes = 15) =>
        Math.Round(minutes / snapMinutes, MidpointRounding.AwayFromZero) * snapMinutes;

    public static bool Intersects(DateTime start, DateTime end, DateTime rangeStart, DateTime rangeEnd) =>
        start < rangeEnd && end > rangeStart;

    public static bool Intersects(IKalendsEvent item, DateTime rangeStart, DateTime rangeEnd) =>
        Intersects(item.Start, item.End, rangeStart, rangeEnd);

    public static IReadOnlyList<DateTime> DaysInRange(DateTime first, int count, bool showWeekends)
    {
        var days = new List<DateTime>(count);
        for (int i = 0; i < count; i++)
        {
            var day = first.AddDays(i);
            if (!showWeekends && day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            days.Add(day);
        }
        return days;
    }

    public static IReadOnlyList<DayOfWeek> VisibleWeekDays(DayOfWeek firstDayOfWeek, bool showWeekends)
    {
        var days = new List<DayOfWeek>(7);
        for (int i = 0; i < 7; i++)
        {
            var day = (DayOfWeek)(((int)firstDayOfWeek + i) % 7);
            if (!showWeekends && day is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            days.Add(day);
        }
        return days;
    }
}
