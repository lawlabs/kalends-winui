namespace Kalends;

/// <summary>Presentation of a date supplied by the host, independently of events.</summary>
public enum KalendsDayKind
{
    Working,
    Weekend,
    Holiday,
    TransferredDayOff,
    TransferredWorking,
}

/// <summary>The host resolves calendar rules; Kalends only renders the result.</summary>
public sealed record KalendsDayInfo
{
    public KalendsDayKind Kind { get; init; }
    public bool IsShortened { get; init; }
    public string? Description { get; init; }

    public bool IsNonWorking => Kind is KalendsDayKind.Weekend or KalendsDayKind.Holiday or KalendsDayKind.TransferredDayOff;

    public bool ShouldHighlight(bool weekends, bool holidays) => Kind switch
    {
        KalendsDayKind.Weekend => weekends,
        KalendsDayKind.Holiday or KalendsDayKind.TransferredDayOff => holidays,
        _ => false,
    };

    public string Marker(bool weekends, bool holidays, bool workingDays, KalendsStrings strings)
    {
        if (ShouldHighlight(weekends, holidays))
            return Kind switch
            {
                KalendsDayKind.Weekend => strings.WeekendMarker,
                KalendsDayKind.Holiday => strings.HolidayMarker,
                _ => strings.TransferredDayOffMarker,
            };
        if (!workingDays || IsNonWorking) return "";
        var parts = new List<string>(2);
        if (Kind == KalendsDayKind.TransferredWorking) parts.Add(strings.WorkingDayMarker);
        if (IsShortened) parts.Add(strings.ShortenedDayMarker);
        return string.Join(" ", parts);
    }
}
