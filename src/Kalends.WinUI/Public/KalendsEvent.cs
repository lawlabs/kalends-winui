namespace Kalends;

/// <summary>Default in-memory event. Applications may use their own <see cref="IKalendsEvent"/> instead.</summary>
public sealed class KalendsEvent : IKalendsEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("D");
    public string Title { get; set; } = "New event";
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public bool IsAllDay { get; set; }
    public KalendsColor Color { get; set; } = KalendsColor.FromRgb(52, 121, 103);
    public bool IsReadOnly { get; set; }
    public string? Subtitle { get; set; }

    public KalendsEvent Copy() => new()
    {
        Id = Id,
        Title = Title,
        Start = Start,
        End = End,
        IsAllDay = IsAllDay,
        Color = Color,
        IsReadOnly = IsReadOnly,
        Subtitle = Subtitle
    };
}
