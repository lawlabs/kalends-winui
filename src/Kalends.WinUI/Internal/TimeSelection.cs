namespace Kalends;

internal static class TimeSelection
{
    internal static int SnapMinute(double y, double hourHeight, int snapMinutes, bool isAnchor)
    {
        int snap = Math.Clamp(snapMinutes, 1, 1440);
        double minute = KalendsDate.Snap(y / Math.Max(1, hourHeight) * 60, snap);
        return (int)Math.Clamp(minute, 0, isAnchor ? 1440 - snap : 1440);
    }

    internal static (DateTime Start, DateTime End) Range(
        DateTime anchor, double originY, double currentY, double hourHeight, int snapMinutes)
    {
        int snap = Math.Clamp(snapMinutes, 1, 1440);
        var cursor = anchor.Date.AddMinutes(SnapMinute(currentY, hourHeight, snap, false));
        var start = anchor < cursor ? anchor : cursor;
        var end = anchor > cursor ? anchor : cursor;
        if (start == end)
        {
            if (currentY < originY && start > anchor.Date) start = start.AddMinutes(-snap);
            else end = end.AddMinutes(snap);
        }
        return (start, end);
    }
}
