namespace Kalends;

/// <summary>Grid metrics. Prefer this over scattered boolean flags when customizing density.</summary>
public sealed class KalendsMetrics
{
    public static KalendsMetrics Default { get; } = new();

    public double TimeGutterWidth { get; init; } = 58;
    public int SnapMinutes { get; init; } = 15;
    public int AgendaDays { get; init; } = 30;

    /// <summary>Time-of-day format for hour labels and event tiles. Default stays 24-hour so the gutter stays aligned.</summary>
    public string TimeFormat { get; init; } = "HH:mm";
}
