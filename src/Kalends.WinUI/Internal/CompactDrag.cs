namespace Kalends;

internal static class CompactDrag
{
    internal static (DateTime Start, DateTime End) MoveToDate(
        IKalendsEvent item, DateTime grabbedDate, DateTime targetDate)
    {
        var delta = targetDate.Date - grabbedDate.Date;
        return (item.Start + delta, item.End + delta);
    }
}
