using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Kalends;

public sealed partial class KalendsView
{
    public static readonly DependencyProperty DayInfoProviderProperty = DependencyProperty.Register(
        nameof(DayInfoProvider), typeof(object), typeof(KalendsView), new PropertyMetadata(null, OnVisualPropertyChanged));
    public static readonly DependencyProperty HighlightWeekendsProperty = DependencyProperty.Register(
        nameof(HighlightWeekends), typeof(bool), typeof(KalendsView), new PropertyMetadata(true, OnVisualPropertyChanged));
    public static readonly DependencyProperty HighlightHolidaysProperty = DependencyProperty.Register(
        nameof(HighlightHolidays), typeof(bool), typeof(KalendsView), new PropertyMetadata(true, OnVisualPropertyChanged));
    public static readonly DependencyProperty ShowWorkingDayMarkersProperty = DependencyProperty.Register(
        nameof(ShowWorkingDayMarkers), typeof(bool), typeof(KalendsView), new PropertyMetadata(true, OnVisualPropertyChanged));

    /// <summary>Synchronous lookup in host-owned data. Return null to use the default Saturday/Sunday pattern.</summary>
    public Func<DateOnly, KalendsDayInfo?>? DayInfoProvider
    {
        get => GetValue(DayInfoProviderProperty) as Func<DateOnly, KalendsDayInfo?>;
        set => SetValue(DayInfoProviderProperty, value);
    }

    public bool HighlightWeekends
    {
        get => (bool)GetValue(HighlightWeekendsProperty);
        set => SetValue(HighlightWeekendsProperty, value);
    }

    public bool HighlightHolidays
    {
        get => (bool)GetValue(HighlightHolidaysProperty);
        set => SetValue(HighlightHolidaysProperty, value);
    }

    public bool ShowWorkingDayMarkers
    {
        get => (bool)GetValue(ShowWorkingDayMarkersProperty);
        set => SetValue(ShowWorkingDayMarkersProperty, value);
    }

    private readonly Dictionary<DateOnly, KalendsDayInfo> _dayInfo = [];

    private KalendsDayInfo DayInfo(DateTime day)
    {
        var date = DateOnly.FromDateTime(day);
        if (_dayInfo.TryGetValue(date, out var info)) return info;
        info = DayInfoProvider?.Invoke(date) ?? new KalendsDayInfo
        {
            Kind = day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ? KalendsDayKind.Weekend : KalendsDayKind.Working,
        };
        _dayInfo.Add(date, info);
        return info;
    }

    private Brush DayBackground(DateTime day)
    {
        var info = DayInfo(day);
        return Brush(info.ShouldHighlight(HighlightWeekends, HighlightHolidays)
            ? info.Kind == KalendsDayKind.Weekend ? "Weekend" : "DayOff"
            : day == DateTime.Today ? "Today" : "Surface");
    }

    private string DayInk(DateTime day, string fallback = "Text")
    {
        var info = DayInfo(day);
        if (info.ShouldHighlight(HighlightWeekends, HighlightHolidays))
            return info.Kind == KalendsDayKind.Weekend ? "WeekendText" : "DayOffText";
        if (ShowWorkingDayMarkers && !info.IsNonWorking && (info.Kind == KalendsDayKind.TransferredWorking || info.IsShortened))
            return "WorkingDayText";
        return fallback;
    }

    private string DayMarker(DateTime day) => DayInfo(day).Marker(HighlightWeekends, HighlightHolidays, ShowWorkingDayMarkers, Strings);

    private string DayTooltip(DateTime day)
    {
        var info = DayInfo(day);
        var description = info.Description;
        if (string.IsNullOrWhiteSpace(description))
        {
            description = info.Kind switch
            {
                KalendsDayKind.Weekend => Strings.Weekend,
                KalendsDayKind.Holiday => Strings.Holiday,
                KalendsDayKind.TransferredDayOff => Strings.TransferredDayOff,
                KalendsDayKind.TransferredWorking => Strings.TransferredWorking,
                _ => "",
            };
            if (info.IsShortened && !info.IsNonWorking) description = string.Join(" · ", new[] { description, Strings.ShortenedDay }.Where(s => s.Length > 0));
        }
        return string.IsNullOrWhiteSpace(description) ? day.ToString("D", Culture) : $"{day.ToString("D", Culture)}\n{description}";
    }

    private void ConfigureDayTarget(FrameworkElement element, DateTime day)
    {
        AutomationProperties.SetAutomationId(element, $"CalendarDate_{day:yyyyMMdd}");
        AutomationProperties.SetName(element, DayTooltip(day).Replace('\n', ' '));
        AutomationProperties.SetHelpText(element, DayTooltip(day));
        ToolTipService.SetToolTip(element, DayTooltip(day));
    }

    private StackPanel DateLabel(DateTime day, UIElement number, double markerSize = 9, bool onBrand = false, bool compact = false)
    {
        var label = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };
        label.Children.Add(number);
        var marker = DayMarker(day);
        if (compact && ShowWorkingDayMarkers && DayInfo(day).IsShortened && !DayInfo(day).IsNonWorking)
            marker = (DayInfo(day).Kind == KalendsDayKind.TransferredWorking ? Strings.WorkingDayMarker : "") + "*";
        if (marker.Length > 0)
            label.Children.Add(Text(marker, markerSize, onBrand ? "OnBrand" : DayInk(day), true));
        return label;
    }
}
