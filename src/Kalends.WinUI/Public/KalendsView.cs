using System.Globalization;
using Microsoft.UI.Input;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.Foundation;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.ViewManagement;

namespace Kalends;

/// <summary>
/// Presentation-only scheduler. The host owns events, edits, undo, and storage.
/// </summary>
public sealed partial class KalendsView : UserControl
{
    public static readonly DependencyProperty DateProperty = DependencyProperty.Register(
        nameof(Date), typeof(DateTime), typeof(KalendsView),
        new PropertyMetadata(DateTime.Today, OnVisualPropertyChanged));

    public static readonly DependencyProperty ModeProperty = DependencyProperty.Register(
        nameof(Mode), typeof(KalendsMode), typeof(KalendsView),
        new PropertyMetadata(KalendsMode.Week, OnVisualPropertyChanged));

    public static readonly DependencyProperty ShowWeekendsProperty = DependencyProperty.Register(
        nameof(ShowWeekends), typeof(bool), typeof(KalendsView),
        new PropertyMetadata(true, OnVisualPropertyChanged));

    public static readonly DependencyProperty ShowDayHeaderProperty = DependencyProperty.Register(
        nameof(ShowDayHeader), typeof(bool), typeof(KalendsView),
        new PropertyMetadata(false, OnVisualPropertyChanged));

    public static readonly DependencyProperty HourHeightProperty = DependencyProperty.Register(
        nameof(HourHeight), typeof(double), typeof(KalendsView),
        new PropertyMetadata(64d, OnVisualPropertyChanged));

    public static readonly DependencyProperty IsSearchingProperty = DependencyProperty.Register(
        nameof(IsSearching), typeof(bool), typeof(KalendsView),
        new PropertyMetadata(false, OnVisualPropertyChanged));

    public static readonly DependencyProperty EventsProperty = DependencyProperty.Register(
        nameof(Events), typeof(object), typeof(KalendsView),
        new PropertyMetadata(Array.Empty<IKalendsEvent>(), OnVisualPropertyChanged));

    public static readonly DependencyProperty SelectedEventIdProperty = DependencyProperty.Register(
        nameof(SelectedEventId), typeof(string), typeof(KalendsView),
        new PropertyMetadata(null, OnSelectedEventIdChanged));

    public DateTime Date
    {
        get => (DateTime)GetValue(DateProperty);
        set => SetValue(DateProperty, value);
    }

    public KalendsMode Mode
    {
        get => (KalendsMode)GetValue(ModeProperty);
        set => SetValue(ModeProperty, value);
    }

    public bool ShowWeekends
    {
        get => (bool)GetValue(ShowWeekendsProperty);
        set => SetValue(ShowWeekendsProperty, value);
    }

    /// <summary>Whether the weekday and date header is shown in Day mode. Week mode always shows day headers.</summary>
    public bool ShowDayHeader
    {
        get => (bool)GetValue(ShowDayHeaderProperty);
        set => SetValue(ShowDayHeaderProperty, value);
    }

    public double HourHeight
    {
        get => (double)GetValue(HourHeightProperty);
        set => SetValue(HourHeightProperty, value);
    }

    public bool IsSearching
    {
        get => (bool)GetValue(IsSearchingProperty);
        set => SetValue(IsSearchingProperty, value);
    }

    public IEnumerable<IKalendsEvent> Events
    {
        get => GetValue(EventsProperty) as IEnumerable<IKalendsEvent> ?? [];
        set => SetValue(EventsProperty, value);
    }

    /// <summary>ID of the selected event, or null when no event is selected.</summary>
    public string? SelectedEventId
    {
        get => (string?)GetValue(SelectedEventIdProperty);
        set => SetValue(SelectedEventIdProperty, value);
    }

    /// <summary>The selected event in the current host-owned collection, if present.</summary>
    public IKalendsEvent? SelectedEvent => SelectedEventId is { } id ? Events.FirstOrDefault(item => item.Id == id) : null;

    public CultureInfo Culture { get; set; } = CultureInfo.CurrentCulture;
    public DayOfWeek? FirstDayOfWeek { get; set; }
    public KalendsStrings Strings { get; set; } = KalendsStrings.Default;
    public KalendsMetrics Metrics { get; set; } = KalendsMetrics.Default;
    public Func<KalendsEventDraft, IKalendsEvent>? CreateEvent { get; set; }

    public event EventHandler<KalendsEventRequestEventArgs>? EditRequested;
    public event EventHandler<KalendsSelectionChangedEventArgs>? SelectionChanged;
    public event EventHandler<KalendsTimeChangedEventArgs>? TimeChanged;
    public event EventHandler<KalendsEventRequestEventArgs>? DuplicateRequested;
    /// <summary>Raised when deletion is requested. The host owns confirmation and removal.</summary>
    public event EventHandler<KalendsEventRequestEventArgs>? DeleteRequested;
    public event EventHandler<KalendsNavigateEventArgs>? NavigateRequested;
    public event EventHandler<string>? StatusChanged;

    private readonly Grid _root = new();
    private readonly DispatcherTimer _resizeTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
    private readonly DispatcherTimer _clock = new() { Interval = TimeSpan.FromMinutes(1) };
    private readonly List<(Button Button, IKalendsEvent Event)> _eventButtons = [];
    private readonly List<(FrameworkElement Element, DateTime Day)> _dateTargets = [];
    private ScrollViewer? _timeScroll;
    private Canvas? _canvas;
    private DateTime[] _viewDays = [];
    private DateTime _rangeStart;
    private DateTime _rangeEnd;
    private double _columnWidth;
    private double _scrollOffset = 8 * 64;
    private bool _gesture;
    private Action? _cancelGesture;
    private DayOfWeek FirstWeekDay => FirstDayOfWeek ?? Culture.DateTimeFormat.FirstDayOfWeek;
    private double Gutter => Metrics.TimeGutterWidth;
    private string FormatTime(DateTime value) => value.ToString(Metrics.TimeFormat, Culture);
    private string FormatDateTime(DateTime value) => $"{value.ToString("d MMM", Culture)} {FormatTime(value)}";

    public KalendsView()
    {
        // The view tree is built in code; the empty XAML is not required at runtime.
        Resources.MergedDictionaries.Add(KalendsTheme.Create());
        Content = _root;
        IsTabStop = false;
        SizeChanged += (_, _) => { _resizeTimer.Stop(); _resizeTimer.Start(); };
        _resizeTimer.Tick += (_, _) => { _resizeTimer.Stop(); if (!_gesture) Refresh(); };
        ActualThemeChanged += (_, _) => Refresh();
        _clock.Tick += (_, _) => { if (!_gesture && Mode is KalendsMode.Day or KalendsMode.Week) Refresh(); };
        Loaded += (_, _) => { _clock.Start(); Refresh(); };
        Unloaded += (_, _) => { _clock.Stop(); _resizeTimer.Stop(); _cancelGesture?.Invoke(); };
        KeyDown += (_, e) =>
        {
            if (e.Key == VirtualKey.Escape)
            {
                if (_gesture) _cancelGesture?.Invoke();
                else if (SelectedEventId != null) SelectedEventId = null;
                else return;
                e.Handled = true;
            }
            else if (e.Key == VirtualKey.Delete && !_gesture && SelectedEvent is { IsReadOnly: false } selected)
            {
                RequestDelete(selected);
                e.Handled = true;
            }
        };
    }

    public Brush Brush(string key)
    {
        var theme = new AccessibilitySettings().HighContrast ? "HighContrast" : ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
        if (TryThemeBrush(Application.Current?.Resources, theme, key, out var fromApp)) return fromApp;
        if (TryThemeBrush(Resources, theme, key, out var fromControl)) return fromControl;
        foreach (var merged in Resources.MergedDictionaries)
        {
            if (TryThemeBrush(merged, theme, key, out var fromMerged)) return fromMerged;
        }
        return new SolidColorBrush(Microsoft.UI.Colors.Transparent);
    }

    private static bool TryThemeBrush(ResourceDictionary? resources, string theme, string key, out Brush brush)
    {
        brush = null!;
        if (resources is null) return false;
        return resources.ThemeDictionaries.TryGetValue(theme, out var boxed)
            && boxed is ResourceDictionary themeDictionary
            && TryFindBrush(themeDictionary, key, out brush);
    }

    private static bool TryFindBrush(ResourceDictionary? dictionary, string key, out Brush brush)
    {
        brush = null!;
        if (dictionary is null) return false;
        if (dictionary.TryGetValue("Kalends" + key + "Brush", out var prefixed) && prefixed is Brush prefixedBrush)
        {
            brush = prefixedBrush;
            return true;
        }
        if (dictionary.TryGetValue(key + "Brush", out var plain) && plain is Brush plainBrush)
        {
            brush = plainBrush;
            return true;
        }
        return false;
    }

    private Brush EventFill(KalendsColor color)
    {
        if (new AccessibilitySettings().HighContrast) return Brush("Surface");
        byte alpha = ActualTheme == ElementTheme.Dark ? (byte)102 : (byte)40;
        return new SolidColorBrush(Windows.UI.Color.FromArgb(alpha, color.R, color.G, color.B));
    }

    private Brush EventInk(KalendsColor color)
    {
        if (new AccessibilitySettings().HighContrast) return Brush("Text");
        if (ActualTheme != ElementTheme.Dark)
            return new SolidColorBrush(Windows.UI.Color.FromArgb(255, color.R, color.G, color.B));
        return new SolidColorBrush(Windows.UI.Color.FromArgb(255,
            (byte)Math.Min(255, color.R + 80),
            (byte)Math.Min(255, color.G + 80),
            (byte)Math.Min(255, color.B + 80)));
    }

    private static readonly FontFamily UiFont = new("Segoe UI");

    private TextBlock Text(string value, double size = 12, string brush = "Text", bool bold = false) => new()
    {
        Text = value,
        FontSize = size,
        FontFamily = UiFont,
        Language = Culture.IetfLanguageTag,
        Foreground = Brush(brush),
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        TextTrimming = TextTrimming.CharacterEllipsis,
        VerticalAlignment = VerticalAlignment.Center
    };

    private TextBlock ColoredText(string value, Brush foreground, double size = 12, bool bold = false) => new()
    {
        Text = value,
        FontSize = size,
        FontFamily = UiFont,
        Language = Culture.IetfLanguageTag,
        Foreground = foreground,
        FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
        TextTrimming = TextTrimming.CharacterEllipsis,
        VerticalAlignment = VerticalAlignment.Center
    };

    private Button ActionButton(object content, Action? action, string? name = null)
    {
        var button = new Button
        {
            Content = content,
            Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent),
            BorderThickness = new Thickness(0),
            Padding = new Thickness(8),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        if (action != null) button.Click += (_, _) => action();
        if (name != null) AutomationProperties.SetName(button, name);
        return button;
    }

    private static void Cell(Grid grid, FrameworkElement child, int row, int col)
    {
        Grid.SetRow(child, row);
        Grid.SetColumn(child, col);
        grid.Children.Add(child);
    }

    private static void Position(Canvas canvas, FrameworkElement child, double x, double y, double width, double height)
    {
        Canvas.SetLeft(child, x);
        Canvas.SetTop(child, y);
        child.Width = Math.Max(1, width);
        child.Height = Math.Max(1, height);
        canvas.Children.Add(child);
    }

    private IEnumerable<IKalendsEvent> OnDay(DateTime day) =>
        Events.Where(e => KalendsDate.Intersects(e, day, day.AddDays(1))).OrderBy(e => !e.IsAllDay).ThenBy(e => e.Start);

    private IKalendsEvent Draft(DateTime start, DateTime end, bool allDay) =>
        CreateEvent?.Invoke(new KalendsEventDraft(start, end, allDay))
        ?? new KalendsEvent { Start = start, End = end, IsAllDay = allDay };

    private void RequestEdit(IKalendsEvent item) => EditRequested?.Invoke(this, new KalendsEventRequestEventArgs(item));
    private void RequestDuplicate(IKalendsEvent item) => DuplicateRequested?.Invoke(this, new KalendsEventRequestEventArgs(item));
    private void RequestDelete(IKalendsEvent item) => DeleteRequested?.Invoke(this, new KalendsEventRequestEventArgs(item));
    private void RequestTime(IKalendsEvent item, DateTime start, DateTime end) =>
        TimeChanged?.Invoke(this, new KalendsTimeChangedEventArgs(item, start, end));
    private void RequestNavigate(DateTime date, KalendsMode mode) =>
        NavigateRequested?.Invoke(this, new KalendsNavigateEventArgs(date, mode));

    private static void OnVisualPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is KalendsView view && view.IsLoaded) view.Refresh();
    }

    private static void OnSelectedEventIdChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not KalendsView view) return;
        view.UpdateSelectionVisuals();
        view.SelectionChanged?.Invoke(view, new KalendsSelectionChangedEventArgs(view.SelectedEvent));
    }

    private void SelectEvent(IKalendsEvent item) => SelectedEventId = item.Id;

    private void RegisterEventButton(Button button, IKalendsEvent item)
    {
        _eventButtons.Add((button, item));
        button.Click += (_, _) =>
        {
            button.Focus(FocusState.Programmatic);
            SelectEvent(item);
        };
        button.DoubleTapped += (_, e) =>
        {
            SelectEvent(item);
            RequestEdit(item);
            e.Handled = true;
        };
        button.GotFocus += (_, _) =>
        {
            if (button.FocusState == FocusState.Keyboard) SelectEvent(item);
        };
        button.AddHandler(KeyDownEvent, new KeyEventHandler((_, e) =>
        {
            if (e.Key != VirtualKey.Enter) return;
            SelectEvent(item);
            RequestEdit(item);
            e.Handled = true;
        }), true);
        var menu = new MenuFlyout();
        menu.Opening += (_, _) => SelectEvent(item);
        var edit = new MenuFlyoutItem { Text = Strings.OpenEvent, Icon = new SymbolIcon(Symbol.Edit) };
        edit.Click += (_, _) => RequestEdit(item);
        menu.Items.Add(edit);
        if (!item.IsReadOnly)
        {
            var duplicate = new MenuFlyoutItem { Text = Strings.DuplicateEvent, Icon = new SymbolIcon(Symbol.Copy) };
            duplicate.Click += (_, _) => RequestDuplicate(item);
            menu.Items.Add(duplicate);
            menu.Items.Add(new MenuFlyoutSeparator());
            var delete = new MenuFlyoutItem { Text = Strings.DeleteEvent, Icon = new SymbolIcon(Symbol.Delete) };
            delete.Click += (_, _) => RequestDelete(item);
            menu.Items.Add(delete);
        }
        button.ContextFlyout = menu;
        UpdateSelectionVisual(button, item);
    }

    private void UpdateSelectionVisuals()
    {
        foreach (var (button, item) in _eventButtons) UpdateSelectionVisual(button, item);
    }

    private void UpdateSelectionVisual(Button button, IKalendsEvent item)
    {
        bool selected = SelectedEventId == item.Id;
        var border = selected ? Brush("Brand") : button is EventTile ? EventInk(item.Color) : Brush("Line");
        button.BorderBrush = border;
        button.BorderThickness = button is EventTile
            ? selected ? new Thickness(3, 2, 2, 2) : new Thickness(3, 0, 0, 0)
            : selected ? new Thickness(2) : new Thickness(0);
        AutomationProperties.SetHelpText(button, selected ? Strings.Selected : string.Empty);
    }

    public void Refresh(bool resetScroll = false)
    {
        if (_gesture || ActualWidth < 50) return;
        var focusedButton = XamlRoot != null ? FocusManager.GetFocusedElement(XamlRoot) as Button : null;
        var focusId = _eventButtons.FirstOrDefault(entry => ReferenceEquals(entry.Button, focusedButton)).Event?.Id;
        _eventButtons.Clear();
        _dateTargets.Clear();
        _dayInfo.Clear();
        if (resetScroll) _scrollOffset = 8 * HourHeight;
        _timeScroll = null;
        _root.Children.Clear();
        _root.RowDefinitions.Clear();
        _root.Background = Brush("Surface");
        if (IsSearching) RenderAgenda();
        else
        {
            switch (Mode)
            {
                case KalendsMode.Day:
                case KalendsMode.Week:
                    RenderTime();
                    break;
                case KalendsMode.Month:
                    RenderMonth();
                    break;
                case KalendsMode.Year:
                    RenderYear();
                    break;
                default:
                    RenderAgenda();
                    break;
            }
        }
        if (SelectedEventId != null && SelectedEvent is null) SelectedEventId = null;
        if (focusId != null)
            DispatcherQueue.TryEnqueue(() => _eventButtons.FirstOrDefault(entry => entry.Event.Id == focusId).Button?.Focus(FocusState.Programmatic));
    }

    private void RenderTime()
    {
        var weekStart = Mode == KalendsMode.Day ? Date.Date : KalendsDate.StartOfWeek(Date, FirstWeekDay);
        _viewDays = Mode == KalendsMode.Day
            ? [Date.Date]
            : KalendsDate.DaysInRange(weekStart, 7, ShowWeekends).ToArray();
        _rangeStart = _viewDays[0];
        _rangeEnd = _viewDays[^1].AddDays(1);
        int days = _viewDays.Length;
        _columnWidth = Math.Max(20, (ActualWidth - Gutter - 14) / days);
        double width = Gutter + days * _columnWidth;
        _root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new() { Height = GridLength.Auto });
        _root.RowDefinitions.Add(new() { Height = new GridLength(1, GridUnitType.Star) });

        if (Mode == KalendsMode.Week || ShowDayHeader)
        {
            var header = new Grid { Height = 76, Width = days * _columnWidth, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(Gutter, 0, 0, 0) };
            for (int d = 0; d < days; d++)
            {
                var day = _viewDays[d];
                header.ColumnDefinitions.Add(new() { Width = new GridLength(_columnWidth) });
                var label = new StackPanel { Spacing = 4, HorizontalAlignment = HorizontalAlignment.Center };
                var weekday = Text(Culture.DateTimeFormat.GetAbbreviatedDayName(day.DayOfWeek).ToUpper(Culture), 10, day == DateTime.Today ? "Brand" : DayInk(day, "Muted"), true);
                weekday.HorizontalAlignment = HorizontalAlignment.Center;
                weekday.TextAlignment = TextAlignment.Center;
                label.Children.Add(weekday);
                var number = new Border { Width = 34, Height = 34, HorizontalAlignment = HorizontalAlignment.Center, CornerRadius = new CornerRadius(17), Background = Brush(day == DateTime.Today ? "Brand" : "Surface") };
                var numberText = Text(day.Day.ToString(Culture), 20, day == DateTime.Today ? "OnBrand" : DayInk(day));
                numberText.HorizontalAlignment = HorizontalAlignment.Center;
                number.Child = numberText;
                label.Children.Add(number);
                if (DayMarker(day) is { Length: > 0 } marker)
                {
                    var markerText = Text(marker, 10, DayInk(day), true);
                    markerText.HorizontalAlignment = HorizontalAlignment.Center;
                    label.Children.Add(markerText);
                }
                var dateTarget = ActionButton(label, () => RequestNavigate(day, KalendsMode.Day), DayTooltip(day));
                dateTarget.Padding = new Thickness(4, 2, 4, 2);
                ConfigureDayTarget(dateTarget, day);
                Cell(header, dateTarget, 0, d);
            }
            _root.Children.Add(header);
        }

        var allDay = new Grid { Width = width, HorizontalAlignment = HorizontalAlignment.Left, Padding = new Thickness(Gutter, 4, 0, 7), BorderBrush = Brush("Line"), BorderThickness = new Thickness(0, 1, 0, 1), MinHeight = 39 };
        var allItems = Events.Where(e => e.IsAllDay && KalendsDate.Intersects(e, _rangeStart, _rangeEnd)).OrderBy(e => e.Start).ThenByDescending(e => e.End).ToArray();
        var occupied = new List<bool[]>();
        var spans = new List<(IKalendsEvent Item, int Lane, int Start, int Length)>();
        foreach (var item in allItems)
        {
            int start = Math.Max(0, (item.Start.Date - _rangeStart).Days);
            int end = Math.Min(days, (item.End.Date - _rangeStart).Days);
            if (end <= start) end = start + 1;
            int lane = occupied.FindIndex(cells => !cells.Skip(start).Take(end - start).Any(x => x));
            if (lane < 0) { lane = occupied.Count; occupied.Add(new bool[days]); }
            for (int i = start; i < end && i < days; i++) occupied[lane][i] = true;
            spans.Add((item, lane, start, end - start));
        }
        for (int r = 0; r < Math.Max(1, occupied.Count); r++) allDay.RowDefinitions.Add(new() { Height = new GridLength(28) });
        // Use the same Canvas coordinates as the hourly grid. Event margins must not inset day shading.
        var allDayBackdrop = new Canvas { Width = width, Height = Math.Max(1, occupied.Count) * 28 + 13, IsHitTestVisible = false };
        for (int d = 0; d < days; d++)
        {
            var day = _viewDays[d];
            allDay.ColumnDefinitions.Add(new() { Width = new GridLength(_columnWidth) });
            Position(allDayBackdrop, new Border
            {
                Background = DayBackground(day),
                BorderBrush = Brush("Line"),
                BorderThickness = new Thickness(1, 0, 0, 0),
            }, Gutter + d * _columnWidth, 0, _columnWidth, allDayBackdrop.Height);
            var stack = new StackPanel { Spacing = 3 };
            ConfigureDrop(stack, day);
            stack.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
            stack.DoubleTapped += (_, e) => { RequestEdit(Draft(day, day.AddDays(1), true)); e.Handled = true; };
            Grid.SetRowSpan(stack, Math.Max(1, occupied.Count));
            Cell(allDay, stack, 0, d);
        }
        foreach (var (item, lane, start, length) in spans)
        {
            var tile = EventButton(item, true);
            tile.Margin = new Thickness(3, 1, 3, 1);
            Grid.SetColumnSpan(tile, Math.Max(1, length));
            Cell(allDay, tile, lane, start);
        }
        var allDayHost = new Grid { Width = width, HorizontalAlignment = HorizontalAlignment.Left, Background = Brush("Surface") };
        allDayHost.Children.Add(allDayBackdrop);
        allDayHost.Children.Add(allDay);
        Cell(_root, allDayHost, 1, 0);
        var allLabel = Text(Strings.AllDayLabel, 8, "Muted");
        allLabel.Margin = new Thickness(10, 0, 0, 0);
        allLabel.IsHitTestVisible = false;
        Cell(_root, allLabel, 1, 0);

        _canvas = new Canvas { Width = width, Height = HourHeight * 24, HorizontalAlignment = HorizontalAlignment.Left, Background = Brush("Surface") };
        for (int d = 0; d < days; d++)
        {
            var day = _viewDays[d];
            var backdrop = new Border
            {
                Background = DayBackground(day),
                BorderBrush = Brush("Line"),
                BorderThickness = new Thickness(1, 0, 0, 0)
            };
            Position(_canvas, backdrop, Gutter + d * _columnWidth, 0, _columnWidth, HourHeight * 24);
            ConfigureTimeSelection(backdrop, _canvas, day, d);
        }
        for (int h = 0; h < 24; h++)
        {
            var hour = Text(FormatTime(new DateTime(1, 1, 1, h, 0, 0)), 10, "Muted");
            Position(_canvas, hour, 10, h * HourHeight + 3, 45, 18);
            Position(_canvas, new Border { Background = Brush("Line"), IsHitTestVisible = false }, Gutter, h * HourHeight, width - Gutter, 1);
            Position(_canvas, new Border { Background = Brush("SubtleLine"), IsHitTestVisible = false }, Gutter, (h + .5) * HourHeight, width - Gutter, 1);
        }
        for (int d = 0; d < days; d++)
        {
            var day = _viewDays[d];
            foreach (var placed in EventLayoutCalculator.Layout(OnDay(day).Where(e => !e.IsAllDay)))
            {
                double start = Math.Max(0, (placed.Event.Start - day).TotalHours);
                double end = Math.Min(24, (placed.Event.End - day).TotalHours);
                double laneWidth = (_columnWidth - 8) / placed.LaneCount;
                var tile = EventButton(placed.Event, false);
                Position(_canvas, tile, Gutter + d * _columnWidth + 4 + placed.Lane * laneWidth, start * HourHeight + 1, laneWidth - 3, Math.Max(18, (end - start) * HourHeight - 3));
                if (!placed.Event.IsReadOnly) ConfigureGesture(tile, placed.Event, day);
            }
        }
        if (DateTime.Today >= _rangeStart && DateTime.Today < _rangeEnd)
        {
            int todayIndex = Array.FindIndex(_viewDays, day => day == DateTime.Today);
            if (todayIndex >= 0)
            {
                double y = DateTime.Now.TimeOfDay.TotalHours * HourHeight;
                double x = Gutter + todayIndex * _columnWidth;
                Position(_canvas, new Border { Background = Brush("Now"), IsHitTestVisible = false }, x, y, _columnWidth, 2);
                Position(_canvas, new Border { Background = Brush("Now"), CornerRadius = new CornerRadius(4), IsHitTestVisible = false }, x - 3, y - 3, 8, 8);
            }
        }
        _timeScroll = new ScrollViewer { Content = _canvas, HorizontalContentAlignment = HorizontalAlignment.Left, VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        var scroll = _timeScroll;
        bool ready = false;
        scroll.Loaded += (_, _) => DispatcherQueue.TryEnqueue(() =>
        {
            if (_timeScroll != scroll) return;
            scroll.ChangeView(null, _scrollOffset, null, true);
            ready = true;
        });
        scroll.ViewChanged += (_, _) => { if (ready && _timeScroll == scroll) _scrollOffset = scroll.VerticalOffset; };
        Cell(_root, scroll, 2, 0);
    }

    private void ConfigureTimeSelection(Border backdrop, Canvas canvas, DateTime day, int column)
    {
        bool active = false, moved = false;
        double originY = 0;
        DateTime anchor = default, start = default, end = default;
        Border? preview = null;

        void Finish()
        {
            if (!active) return;
            active = false;
            _gesture = false;
            _cancelGesture = null;
            backdrop.ReleasePointerCaptures();
            if (preview != null) canvas.Children.Remove(preview);
            preview = null;
        }

        void Update(double y)
        {
            (start, end) = TimeSelection.Range(anchor, originY, y, HourHeight, Metrics.SnapMinutes);

            if (preview == null)
            {
                preview = new Border
                {
                    Background = Brush("Selection"),
                    BorderBrush = Brush("Brand"),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(4),
                    Padding = new Thickness(7, 2, 7, 2),
                    IsHitTestVisible = false
                };
                Canvas.SetZIndex(preview, 20);
                canvas.Children.Add(preview);
            }
            Canvas.SetLeft(preview, Gutter + column * _columnWidth + 4);
            Canvas.SetTop(preview, (start - day).TotalHours * HourHeight + 1);
            preview.Width = _columnWidth - 8;
            preview.Height = Math.Max(14, (end - start).TotalHours * HourHeight - 2);
            preview.Child = ColoredText($"{FormatTime(start)} – {FormatTime(end)}", Brush("Text"), 11, true);
        }

        backdrop.PointerPressed += (_, e) =>
        {
            if (_gesture || e.Pointer.PointerDeviceType != PointerDeviceType.Mouse ||
                !e.GetCurrentPoint(backdrop).Properties.IsLeftButtonPressed) return;
            var point = e.GetCurrentPoint(canvas).Position;
            if (!backdrop.CapturePointer(e.Pointer)) return;
            originY = point.Y;
            anchor = day.AddMinutes(TimeSelection.SnapMinute(point.Y, HourHeight, Metrics.SnapMinutes, true));
            start = anchor;
            end = anchor.AddHours(1);
            active = true;
            moved = false;
            _gesture = true;
            _cancelGesture = Finish;
            e.Handled = true;
        };
        backdrop.PointerMoved += (_, e) =>
        {
            if (!active) return;
            var point = e.GetCurrentPoint(canvas).Position;
            if (!moved && Math.Abs(point.Y - originY) < 5) return;
            moved = true;
            Update(point.Y);
            if (_timeScroll is { } scroll && ReferenceEquals(scroll.Content, canvas))
            {
                double viewY = e.GetCurrentPoint(scroll).Position.Y;
                if (viewY < 35) scroll.ChangeView(null, Math.Max(0, scroll.VerticalOffset - 14), null, true);
                if (viewY > scroll.ActualHeight - 35) scroll.ChangeView(null, scroll.VerticalOffset + 14, null, true);
            }
            e.Handled = true;
        };
        backdrop.PointerReleased += (_, e) =>
        {
            if (!active) return;
            var point = e.GetCurrentPoint(canvas).Position;
            if (Math.Abs(point.Y - originY) >= 5) moved = true;
            if (moved) Update(point.Y);
            Finish();
            SelectedEventId = null;
            if (moved) RequestEdit(Draft(start, end, false));
            e.Handled = true;
        };
        backdrop.DoubleTapped += (_, e) =>
        {
            var point = e.GetPosition(canvas);
            var start = day.AddMinutes(TimeSelection.SnapMinute(point.Y, HourHeight, Metrics.SnapMinutes, true));
            Finish();
            SelectedEventId = null;
            RequestEdit(Draft(start, start.AddHours(1), false));
            e.Handled = true;
        };
        backdrop.PointerCanceled += (_, _) => Finish();
        backdrop.PointerCaptureLost += (_, _) => Finish();
    }

    private EventTile EventButton(IKalendsEvent item, bool compact)
    {
        var ink = EventInk(item.Color);
        var fill = EventFill(item.Color);
        var content = new StackPanel { Spacing = 3, HorizontalAlignment = HorizontalAlignment.Stretch };
        if (!compact) content.Children.Add(ColoredText($"{FormatTime(item.Start)} – {FormatTime(item.End)}", ink, 10));
        var title = ColoredText((compact && !item.IsAllDay ? $"{FormatTime(item.Start)}  " : "") + item.Title, ink, compact ? 11 : 12, true);
        if (!compact) { title.TextWrapping = TextWrapping.Wrap; title.MaxLines = 2; }
        content.Children.Add(title);
        if (!compact && !string.IsNullOrEmpty(item.Subtitle) && (item.End - item.Start).TotalMinutes >= 75)
            content.Children.Add(ColoredText(item.Subtitle, ink, 10));
        var button = new EventTile
        {
            Tag = item.Id,
            Content = content,
            Background = fill,
            Foreground = ink,
            BorderBrush = ink,
            BorderThickness = new Thickness(3, 0, 0, 0),
            CornerRadius = new CornerRadius(4),
            Padding = compact ? new Thickness(7, 4, 5, 4) : new Thickness(8, 5, 5, 4),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Top,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MinWidth = 0,
            MinHeight = 0
        };
        if (!compact)
        {
            if ((item.End - item.Start).TotalMinutes < 45)
            {
                content.Children.Clear();
                content.Children.Add(ColoredText($"{FormatTime(item.Start)}  {item.Title}", ink, 10, true));
            }
        }
        foreach (var state in new[] { "PointerOver", "Pressed" })
        {
            button.Resources[$"ButtonBackground{state}"] = fill;
            button.Resources[$"ButtonForeground{state}"] = ink;
        }
        RegisterEventButton(button, item);
        var when = item.IsAllDay ? Strings.AllDay : $"{FormatDateTime(item.Start)} – {FormatDateTime(item.End)}";
        AutomationProperties.SetName(button, $"{item.Title}, {when}");
        AutomationProperties.SetAutomationId(button, "Event_" + item.Id);
        ToolTipService.SetToolTip(button, string.Format(Culture, Strings.EventTooltip, item.Title, when, item.Subtitle ?? ""));
        if (compact && !item.IsReadOnly) ConfigureCompactGesture(button, item);
        button.KeyDown += (_, e) =>
        {
            if (item.IsReadOnly || !Down(VirtualKey.Menu) || e.Key is not (VirtualKey.Up or VirtualKey.Down or VirtualKey.Left or VirtualKey.Right)) return;
            var delta = e.Key is VirtualKey.Left or VirtualKey.Right
                ? TimeSpan.FromDays(e.Key == VirtualKey.Left ? -1 : 1)
                : item.IsAllDay ? TimeSpan.FromDays(e.Key == VirtualKey.Up ? -1 : 1) : TimeSpan.FromMinutes(e.Key == VirtualKey.Up ? -Metrics.SnapMinutes : Metrics.SnapMinutes);
            var start = Down(VirtualKey.Shift) ? item.Start : item.Start + delta;
            var end = item.End + delta;
            if (end > start) RequestTime(item, start, end);
            e.Handled = true;
        };
        return button;
    }

    private static bool Down(VirtualKey key) =>
        (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;

    private void ConfigureDrop(FrameworkElement target, DateTime day) => _dateTargets.Add((target, day));

    private void ConfigureCompactGesture(EventTile tile, IKalendsEvent item)
    {
        Point origin = default;
        bool active = false, moved = false;
        DateTime? targetDay = null;
        Canvas? layer = null;
        Border? preview = null;
        void Cleanup()
        {
            active = false;
            _gesture = false;
            _cancelGesture = null;
            tile.Opacity = 1;
            tile.ReleasePointerCaptures();
            if (layer != null) _root.Children.Remove(layer);
            layer = null;
            preview = null;
        }
        tile.Press = e =>
        {
            origin = e.GetCurrentPoint(_root).Position;
            active = true;
            moved = false;
            targetDay = null;
            _gesture = true;
            _cancelGesture = () => { Cleanup(); StatusChanged?.Invoke(this, Strings.DragCancelled); };
            tile.CapturePointer(e.Pointer);
            tile.Focus(FocusState.Pointer);
            e.Handled = true;
        };
        tile.Move = e =>
        {
            tile.SetCursor(InputSystemCursorShape.SizeAll);
            if (!active) return;
            var point = e.GetCurrentPoint(_root).Position;
            if (!moved && Math.Abs(point.X - origin.X) + Math.Abs(point.Y - origin.Y) < 5) return;
            moved = true;
            tile.Opacity = .45;
            targetDay = null;
            foreach (var (target, day) in _dateTargets)
            {
                var corner = target.TransformToVisual(_root).TransformPoint(new Point());
                if (new Rect(corner.X, corner.Y, target.ActualWidth, target.ActualHeight).Contains(point))
                {
                    targetDay = day;
                    break;
                }
            }
            if (layer == null)
            {
                layer = new Canvas { IsHitTestVisible = false };
                Grid.SetRowSpan(layer, Math.Max(1, _root.RowDefinitions.Count));
                preview = new Border
                {
                    Background = EventFill(item.Color),
                    BorderBrush = EventInk(item.Color),
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(5),
                    Padding = new Thickness(10),
                    Width = 210,
                    Child = ColoredText(item.Title, EventInk(item.Color), 12, true)
                };
                layer.Children.Add(preview);
                _root.Children.Add(layer);
            }
            Canvas.SetLeft(preview!, Math.Clamp(point.X + 12, 0, Math.Max(0, ActualWidth - 210)));
            Canvas.SetTop(preview!, Math.Max(0, point.Y - 35));
            StatusChanged?.Invoke(this, targetDay.HasValue
                ? Strings.FormatDragToDate(targetDay.Value.ToString("d", Culture), Culture)
                : Strings.DragToDate);
            e.Handled = true;
        };
        tile.Release = e =>
        {
            if (!active) return;
            Cleanup();
            if (moved && targetDay.HasValue)
            {
                var start = targetDay.Value + item.Start.TimeOfDay;
                if (start != item.Start) RequestTime(item, start, start + (item.End - item.Start));
            }
            else if (!moved)
            {
                tile.Focus(FocusState.Programmatic);
                SelectEvent(item);
            }
            e.Handled = true;
        };
        tile.PointerCanceled += (_, _) => { if (active) Cleanup(); };
        tile.PointerCaptureLost += (_, _) => { if (active) Cleanup(); };
    }

    private void ConfigureGesture(EventTile tile, IKalendsEvent item, DateTime segmentDay)
    {
        Point origin = default;
        DateTime nextStart = item.Start, nextEnd = item.End;
        bool active = false, moved = false;
        int edge = 0;
        Border? ghost = null;
        void Cancel()
        {
            if (!active) return;
            active = false;
            _gesture = false;
            _cancelGesture = null;
            tile.Opacity = 1;
            tile.ReleasePointerCaptures();
            if (ghost != null) _canvas?.Children.Remove(ghost);
            StatusChanged?.Invoke(this, Strings.DragCancelled);
        }
        tile.Press = e =>
        {
            if (!e.GetCurrentPoint(tile).Properties.IsLeftButtonPressed || _canvas == null) return;
            var local = e.GetCurrentPoint(tile).Position;
            edge = local.Y < 7 && item.Start >= segmentDay ? -1 : local.Y > tile.ActualHeight - 7 && item.End <= segmentDay.AddDays(1) ? 1 : 0;
            origin = e.GetCurrentPoint(_canvas).Position;
            active = true;
            moved = false;
            _gesture = true;
            _cancelGesture = Cancel;
            nextStart = item.Start;
            nextEnd = item.End;
            tile.CapturePointer(e.Pointer);
            tile.Focus(FocusState.Pointer);
            e.Handled = true;
        };
        tile.Move = e =>
        {
            var local = e.GetCurrentPoint(tile).Position;
            tile.SetCursor(local.Y < 7 || local.Y > tile.ActualHeight - 7 ? InputSystemCursorShape.SizeNorthSouth : InputSystemCursorShape.SizeAll);
            if (!active || _canvas == null) return;
            var p = e.GetCurrentPoint(_canvas).Position;
            if (!moved && Math.Abs(p.X - origin.X) + Math.Abs(p.Y - origin.Y) < 5) return;
            moved = true;
            tile.Opacity = .4;
            double minutes = KalendsDate.Snap((p.Y - origin.Y) / HourHeight * 60, Metrics.SnapMinutes);
            if (edge == 0)
            {
                int oldDay = Math.Max(0, Array.IndexOf(_viewDays, segmentDay.Date));
                int newDay = Math.Clamp((int)((p.X - Gutter) / _columnWidth), 0, _viewDays.Length - 1);
                var delta = TimeSpan.FromDays(newDay - oldDay) + TimeSpan.FromMinutes(minutes);
                (nextStart, nextEnd) = EventLayoutCalculator.Move(item, delta, _rangeStart, _rangeEnd);
            }
            else if (edge == -1) nextStart = new[] { item.Start.AddMinutes(minutes), item.End.AddMinutes(-Metrics.SnapMinutes) }.Min();
            else nextEnd = new[] { item.End.AddMinutes(minutes), item.Start.AddMinutes(Metrics.SnapMinutes) }.Max();
            if (edge != 0)
            {
                nextStart = nextStart < _rangeStart ? _rangeStart : nextStart;
                nextEnd = nextEnd > _rangeEnd ? _rangeEnd : nextEnd;
            }
            if (nextEnd <= nextStart) nextEnd = nextStart.AddMinutes(Metrics.SnapMinutes);
            if (ghost == null)
            {
                ghost = new Border { Background = EventFill(item.Color), BorderBrush = EventInk(item.Color), BorderThickness = new Thickness(2), CornerRadius = new CornerRadius(5), Padding = new Thickness(7), IsHitTestVisible = false };
                Canvas.SetZIndex(ghost, 20);
                _canvas.Children.Add(ghost);
            }
            var visibleDay = nextStart.Date < _rangeStart ? _rangeStart : nextStart.Date;
            int column = Math.Clamp(Array.FindIndex(_viewDays, day => day == visibleDay), 0, _viewDays.Length - 1);
            Canvas.SetLeft(ghost, Gutter + column * _columnWidth + 4);
            Canvas.SetTop(ghost, nextStart.TimeOfDay.TotalHours * HourHeight);
            ghost.Width = _columnWidth - 9;
            ghost.Height = Math.Max(22, (Math.Min(24, (nextEnd - visibleDay).TotalHours) - nextStart.TimeOfDay.TotalHours) * HourHeight);
            ghost.Child = ColoredText($"{FormatTime(nextStart)} – {FormatTime(nextEnd)}\n{item.Title}", EventInk(item.Color), 11, true);
            StatusChanged?.Invoke(this, Strings.FormatDragResize(
                FormatDateTime(nextStart), FormatTime(nextEnd), (nextEnd - nextStart).TotalMinutes, Culture));
            if (_timeScroll != null)
            {
                var viewY = e.GetCurrentPoint(_timeScroll).Position.Y;
                if (viewY < 35) _timeScroll.ChangeView(null, Math.Max(0, _timeScroll.VerticalOffset - 14), null, true);
                if (viewY > _timeScroll.ActualHeight - 35) _timeScroll.ChangeView(null, _timeScroll.VerticalOffset + 14, null, true);
            }
            e.Handled = true;
        };
        tile.Release = e =>
        {
            if (!active) return;
            active = false;
            _gesture = false;
            _cancelGesture = null;
            tile.ReleasePointerCaptures();
            tile.Opacity = 1;
            if (ghost != null) _canvas?.Children.Remove(ghost);
            if (moved)
            {
                if (nextStart != item.Start || nextEnd != item.End) RequestTime(item, nextStart, nextEnd);
                else Refresh();
            }
            else
            {
                tile.Focus(FocusState.Programmatic);
                SelectEvent(item);
            }
            e.Handled = true;
        };
        tile.PointerCanceled += (_, _) => Cancel();
        tile.PointerCaptureLost += (_, _) => { if (active) Cancel(); };
    }

    private void RenderMonth()
    {
        var weekDays = KalendsDate.VisibleWeekDays(FirstWeekDay, ShowWeekends);
        int columns = weekDays.Count;
        _root.RowDefinitions.Add(new() { Height = new GridLength(36) });
        _root.RowDefinitions.Add(new());
        var grid = new Grid();
        var header = new Grid();
        for (int c = 0; c < columns; c++)
        {
            grid.ColumnDefinitions.Add(new());
            header.ColumnDefinitions.Add(new());
            var label = Text(Culture.DateTimeFormat.GetDayName(weekDays[c]).ToUpper(Culture), 9, "Muted", true);
            label.Margin = new Thickness(12, 0, 0, 0);
            Cell(header, label, 0, c);
        }
        DateTime first = KalendsDate.MonthGridStart(Date, FirstWeekDay);
        for (int r = 0; r < 6; r++)
        {
            grid.RowDefinitions.Add(new());
            var weekStart = first.AddDays(r * 7);
            for (int c = 0; c < columns; c++)
            {
                var day = weekStart;
                while (day.DayOfWeek != weekDays[c]) day = day.AddDays(1);
                var panel = new StackPanel { Spacing = 3, Margin = new Thickness(6) };
                var dateButton = ActionButton(
                    DateLabel(day, Text(day.Day == 1 ? day.ToString("d MMM", Culture) : day.Day.ToString(Culture), 12, day == DateTime.Today ? "OnBrand" : DayInk(day, day.Month == Date.Month ? "Text" : "Muted"), true), onBrand: day == DateTime.Today),
                    () => RequestNavigate(day, KalendsMode.Day),
                    day.ToString("D", Culture));
                dateButton.HorizontalAlignment = HorizontalAlignment.Left;
                dateButton.Padding = new Thickness(7, 3, 7, 3);
                if (day == DateTime.Today) dateButton.Background = Brush("Brand");
                ConfigureDayTarget(dateButton, day);
                panel.Children.Add(dateButton);
                var items = OnDay(day).ToArray();
                int max = Math.Clamp((int)((ActualHeight - 36) / 6 - 43) / 27, 1, 5);
                foreach (var item in items.Take(max)) panel.Children.Add(EventButton(item, true));
                if (items.Length > max)
                {
                    var more = ActionButton(Text(Strings.FormatMore(items.Length - max, Culture), 10, "Muted"), () => RequestNavigate(day, KalendsMode.Day));
                    more.Padding = new Thickness(6, 1, 6, 1);
                    panel.Children.Add(more);
                }
                var border = new Border
                {
                    Child = panel,
                    Background = DayBackground(day),
                    BorderBrush = Brush("Line"),
                    BorderThickness = new Thickness(0, 1, 1, 0)
                };
                ConfigureDrop(border, day);
                border.DoubleTapped += (_, e) => { RequestEdit(Draft(day.AddHours(9), day.AddHours(10), false)); e.Handled = true; };
                Cell(grid, border, r, c);
            }
        }
        _root.Children.Add(header);
        Cell(_root, grid, 1, 0);
    }

    private void RenderYear()
    {
        int columns = ActualWidth >= 1000 ? 4 : ActualWidth >= 680 ? 3 : 2;
        var grid = new Grid { Padding = new Thickness(20, 12, 20, 20), ColumnSpacing = 24, RowSpacing = 20 };
        for (int c = 0; c < columns; c++) grid.ColumnDefinitions.Add(new());
        for (int r = 0; r < (int)Math.Ceiling(12.0 / columns); r++) grid.RowDefinitions.Add(new() { Height = GridLength.Auto });
        var weekDays = KalendsDate.VisibleWeekDays(FirstWeekDay, true);
        for (int month = 1; month <= 12; month++)
        {
            var first = new DateTime(Date.Year, month, 1);
            var stack = new StackPanel { Spacing = 6 };
            stack.Children.Add(ActionButton(
                Text(Culture.TextInfo.ToTitleCase(first.ToString("MMMM", Culture)), 17, month == DateTime.Today.Month && Date.Year == DateTime.Today.Year ? "Brand" : "Text", true),
                () => RequestNavigate(first, KalendsMode.Month)));
            var days = new Grid();
            for (int c = 0; c < 7; c++) days.ColumnDefinitions.Add(new());
            for (int r = 0; r < 7; r++) days.RowDefinitions.Add(new() { Height = new GridLength(28) });
            for (int c = 0; c < 7; c++)
            {
                var name = Culture.DateTimeFormat.GetShortestDayName(weekDays[c]);
                var t = Text(name, 10, "Muted");
                t.HorizontalAlignment = HorizontalAlignment.Center;
                Cell(days, t, 0, c);
            }
            var gridStart = KalendsDate.MonthGridStart(first, FirstWeekDay);
            for (int i = 0; i < 42; i++)
            {
                var day = gridStart.AddDays(i);
                if (day.Month != month) continue;
                var content = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
                content.Children.Add(DateLabel(day, Text(day.Day.ToString(Culture), 11, day == DateTime.Today ? "OnBrand" : DayInk(day)), markerSize: 8, onBrand: day == DateTime.Today, compact: true));
                if (OnDay(day).Any())
                    content.Children.Add(new Border { Height = 3, Width = 3, Margin = new Thickness(0, 2, 0, 3), CornerRadius = new CornerRadius(2), Background = Brush(day == DateTime.Today ? "OnBrand" : "Brand"), HorizontalAlignment = HorizontalAlignment.Center });
                var button = ActionButton(content, () => RequestNavigate(day, KalendsMode.Day), day.ToString("D", Culture));
                button.Padding = new Thickness(0);
                button.MinWidth = 0;
                button.MinHeight = 0;
                button.Background = DayBackground(day);
                ConfigureDayTarget(button, day);
                if (day == DateTime.Today)
                {
                    var brand = Brush("Brand");
                    button.Background = brand;
                    button.Resources["ButtonBackgroundPointerOver"] = brand;
                    button.Resources["ButtonBackgroundPressed"] = brand;
                }
                Cell(days, button, i / 7 + 1, i % 7);
            }
            stack.Children.Add(days);
            Cell(grid, stack, (month - 1) / columns, (month - 1) % columns);
        }
        _root.Children.Add(new ScrollViewer { Content = grid, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
    }

    private void RenderAgenda()
    {
        var list = new ListView { SelectionMode = ListViewSelectionMode.None, Padding = new Thickness(28, 8, 28, 24) };
        int count = 0;
        IEnumerable<DateTime> dates = IsSearching
            ? Events.Select(e => e.Start.Date).Distinct().Order()
            : Enumerable.Range(0, Metrics.AgendaDays).Select(d => Date.Date.AddDays(d));
        foreach (var day in dates)
        {
            if (!IsSearching && !ShowWeekends && day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            var items = (IsSearching ? Events.Where(e => e.Start.Date == day).OrderBy(e => e.Start) : OnDay(day)).ToArray();
            if (items.Length == 0) continue;
            var section = new StackPanel { Spacing = 8, Margin = new Thickness(0, 12, 0, 12), HorizontalAlignment = HorizontalAlignment.Stretch };
            var dateHeading = DateLabel(day, Text((day == DateTime.Today ? Strings.Today + " · " : "") + day.ToString("D", Culture), 16, day == DateTime.Today ? "Brand" : DayInk(day), true), markerSize: 11);
            ToolTipService.SetToolTip(dateHeading, DayTooltip(day));
            AutomationProperties.SetName(dateHeading, DayTooltip(day).Replace('\n', ' '));
            AutomationProperties.SetHelpText(dateHeading, DayTooltip(day));
            section.Children.Add(dateHeading);
            foreach (var item in items)
            {
                var ink = EventInk(item.Color);
                var row = new Grid { ColumnSpacing = 20, Padding = new Thickness(4, 10, 4, 10) };
                row.ColumnDefinitions.Add(new() { Width = new GridLength(110) });
                row.ColumnDefinitions.Add(new() { Width = new GridLength(4) });
                row.ColumnDefinitions.Add(new());
                Cell(row, Text(item.IsAllDay ? Strings.AllDay : $"{FormatTime(item.Start)} — {FormatTime(item.End)}", 12, "Muted"), 0, 0);
                Cell(row, new Border { Background = ink, CornerRadius = new CornerRadius(2), Height = 34 }, 0, 1);
                var details = new StackPanel { Spacing = 4 };
                details.Children.Add(Text(item.Title, 14, "Text", true));
                if (!string.IsNullOrEmpty(item.Subtitle)) details.Children.Add(Text(item.Subtitle, 11, "Muted"));
                Cell(row, details, 0, 2);
                var button = ActionButton(row, null, item.Title);
                RegisterEventButton(button, item);
                AutomationProperties.SetAutomationId(button, "Event_" + item.Id);
                section.Children.Add(button);
                count++;
            }
            section.Children.Add(new Border { Height = 1, Background = Brush("Line"), Margin = new Thickness(0, 5, 0, 0) });
            list.Items.Add(new ListViewItem { Content = section, HorizontalContentAlignment = HorizontalAlignment.Stretch, IsTabStop = false, Padding = new Thickness(0) });
        }
        if (count == 0)
        {
            var empty = new StackPanel { Spacing = 14, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };
            empty.Children.Add(Text(IsSearching ? Strings.EmptySearchTitle : Strings.EmptyTitle, 24, "Text", true));
            empty.Children.Add(Text(Strings.EmptySubtitle, 13, "Muted"));
            empty.Children.Add(ActionButton(Strings.CreateEvent, () => RequestEdit(Draft(Date.Date.AddHours(9), Date.Date.AddHours(10), false))));
            _root.Children.Add(empty);
        }
        else _root.Children.Add(list);
    }
}
