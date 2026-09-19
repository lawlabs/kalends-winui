namespace Kalends;

/// <summary>Host-owned calendar used for filters and color. Kalends does not store calendars.</summary>
public interface IKalendsCalendar
{
    string Id { get; }
    string Name { get; }
    KalendsColor Color { get; }
    bool IsVisible { get; }
}
