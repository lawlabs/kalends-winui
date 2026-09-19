namespace Kalends;

/// <summary>Default in-memory calendar.</summary>
public sealed class KalendsCalendar : IKalendsCalendar
{
    public string Id { get; init; } = Guid.NewGuid().ToString("D");
    public string Name { get; set; } = "Calendar";
    public KalendsColor Color { get; set; } = KalendsColor.FromRgb(52, 121, 103);
    public bool IsVisible { get; set; } = true;
}
