using System.Globalization;

namespace Kalends;

/// <summary>User-facing copy. Defaults are English; the host replaces this for other locales.</summary>
public sealed class KalendsStrings
{
    public static KalendsStrings Default { get; } = new();

    public string AllDay { get; init; } = "All day";
    public string AllDayLabel { get; init; } = "ALL\nDAY";
    public string OpenEvent { get; init; } = "Open event";
    public string Selected { get; init; } = "Selected";
    public string DuplicateEvent { get; init; } = "Duplicate";
    public string DeleteEvent { get; init; } = "Delete";
    public string Today { get; init; } = "Today";
    public string Weekend { get; init; } = "Day off";
    public string Holiday { get; init; } = "Public holiday";
    public string TransferredDayOff { get; init; } = "Transferred day off";
    public string TransferredWorking { get; init; } = "Working day due to a transfer";
    public string ShortenedDay { get; init; } = "Shortened working day";
    public string WeekendMarker { get; init; } = "Off";
    public string HolidayMarker { get; init; } = "H";
    public string TransferredDayOffMarker { get; init; } = "↔";
    public string WorkingDayMarker { get; init; } = "W";
    public string ShortenedDayMarker { get; init; } = "−1h";
    public string MoreEvents { get; init; } = "More {0}";
    public string EmptyTitle { get; init; } = "Free time ahead";
    public string EmptySearchTitle { get; init; } = "Nothing found";
    public string EmptySubtitle { get; init; } = "Change the search or create an event.";
    public string CreateEvent { get; init; } = "+ Create event";
    public string DragCancelled { get; init; } = "Move cancelled";
    public string DragToDate { get; init; } = "Drop the event on a calendar date";
    public string DragToDateHint { get; init; } = "Move to {0} · Esc to cancel";
    public string DragResizeHint { get; init; } = "{0} — {1} · {2} min · Esc to cancel";
    public string EventTooltip { get; init; } = "{0}\n{1}\n{2}\nDrag the event or its top / bottom edge.\nAlt+arrows move · Alt+Shift+Up/Down resizes";

    public string FormatMore(int extra, IFormatProvider? culture = null) =>
        string.Format(culture ?? CultureInfo.CurrentCulture, MoreEvents, extra);

    public string FormatDragToDate(string date, IFormatProvider? culture = null) =>
        string.Format(culture ?? CultureInfo.CurrentCulture, DragToDateHint, date);

    public string FormatDragResize(string start, string end, double minutes, IFormatProvider? culture = null) =>
        string.Format(culture ?? CultureInfo.CurrentCulture, DragResizeHint, start, end, minutes);
}
