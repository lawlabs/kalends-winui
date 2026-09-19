namespace Kalends;

/// <summary>
/// Host-owned event shown by Kalends. End is exclusive, including all-day events.
/// Persistence and transport fields stay in the application model.
/// </summary>
public interface IKalendsEvent
{
    string Id { get; }
    string Title { get; }
    DateTime Start { get; }
    DateTime End { get; }
    bool IsAllDay { get; }
    KalendsColor Color { get; }
    bool IsReadOnly { get; }
    string? Subtitle { get; }
}
