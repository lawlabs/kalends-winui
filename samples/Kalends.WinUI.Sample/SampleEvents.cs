using Kalends;

namespace Kalends_WinUI_Sample;

internal static class SampleEvents
{
    public static readonly KalendsColor Work = KalendsColor.FromRgb(101, 81, 143);
    public static readonly KalendsColor Meetings = KalendsColor.FromRgb(56, 109, 174);
    public static readonly KalendsColor Personal = KalendsColor.FromRgb(53, 118, 93);
    public static readonly KalendsColor Important = KalendsColor.FromRgb(151, 102, 30);

    public static List<KalendsEvent> Create(DateTime today)
    {
        var week = KalendsDate.StartOfWeek(today, DayOfWeek.Monday);
        var events = new List<KalendsEvent>();
        void Add(int day, double hour, double duration, string title, KalendsColor color, string? subtitle = null) =>
            events.Add(new()
            {
                Title = title,
                Start = week.AddDays(day).AddHours(hour),
                End = week.AddDays(day).AddHours(hour + duration),
                Color = color,
                Subtitle = subtitle
            });

        Add(0, 9, 1, "Weekly planning", Work, "Office");
        Add(0, 11, 1.5, "Deep work", Work);
        Add(0, 14, 1, "Team sync", Meetings, "Online");
        Add(0, 17, 1, "Training", Personal);
        Add(1, 9.5, 1.5, "Draft documents", Work);
        Add(1, 12, 1, "Lunch", Personal);
        Add(1, 14, 2, "Project review", Meetings);
        Add(2, 9, 1, "Focus block", Work);
        Add(2, 10.5, 1.5, "Consultation", Meetings, "Online");
        Add(2, 15, 1.5, "Research", Work);
        Add(3, 9, 2, "No-meeting morning", Work);
        Add(3, 12, 1, "Client meeting", Meetings);
        Add(3, 15, 1, "Walk", Personal);
        Add(4, 9.5, 1, "Weekly review", Work);
        Add(4, 11, 1.5, "Presentation", Meetings, "Online");
        Add(4, 17, 1.5, "Friends", Personal);
        events.Add(new() { Title = "Project due", Start = week.AddDays(4), End = week.AddDays(5), IsAllDay = true, Color = Important });
        events.Add(new() { Title = "Design days", Start = week.AddDays(1), End = week.AddDays(3), IsAllDay = true, Color = Meetings });
        return events;
    }
}
