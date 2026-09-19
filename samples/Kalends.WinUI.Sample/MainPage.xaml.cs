using System.Globalization;
using Kalends;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Kalends_WinUI_Sample;

public sealed partial class MainPage : Page
{
    private static readonly CultureInfo DisplayCulture = CultureInfo.GetCultureInfo("en-US");
    private readonly List<KalendsEvent> _events = SampleEvents.Create(DateTime.Today);
    private DateTime _date = DateTime.Today;
    private KalendsMode _mode = KalendsMode.Week;
    private bool _ready;

    public MainPage()
    {
        InitializeComponent();
        Calendar.Culture = DisplayCulture;
        Calendar.CreateEvent = draft => new KalendsEvent { Start = draft.Start, End = draft.End, IsAllDay = draft.IsAllDay, Color = SampleEvents.Work };
        Calendar.EditRequested += (_, e) =>
        {
            if (e.Event is KalendsEvent item && !_events.Contains(item)) _events.Add(item);
            StatusText.Text = "Edit requested: " + e.Event.Title;
        };
        Calendar.TimeChanged += (_, e) =>
        {
            if (e.Event is KalendsEvent item)
            {
                item.Start = e.Start;
                item.End = e.End;
                Update();
                StatusText.Text = $"{item.Title}: {e.Start:dd.MM HH:mm} — {e.End:HH:mm}";
            }
        };
        Calendar.DuplicateRequested += (_, e) =>
        {
            if (e.Event is not KalendsEvent item) return;
            var copy = item.Copy();
            copy.Id = Guid.NewGuid().ToString("D");
            copy.Title = item.Title + " copy";
            _events.Add(copy);
            Update();
        };
        Calendar.NavigateRequested += (_, e) => { _date = e.Date; _mode = e.Mode; SyncSelector(); Update(true); };
        Calendar.StatusChanged += (_, text) => StatusText.Text = text;
        _ready = true;
        Update(true);
    }

    private void Update(bool resetScroll = false)
    {
        if (!_ready) return;
        PeriodTitle.Text = _mode switch
        {
            KalendsMode.Day => _date.ToString("D", DisplayCulture),
            KalendsMode.Year => _date.Year.ToString(DisplayCulture),
            _ => _date.ToString("Y", DisplayCulture)
        };
        Calendar.Date = _date;
        Calendar.Mode = _mode;
        Calendar.Events = _events;
        Calendar.Refresh(resetScroll);
    }

    private void SyncSelector()
    {
        ViewSelector.SelectedItem = ViewSelector.Items.First(item => (string)item.Tag == _mode.ToString());
    }

    private void Navigate(int direction)
    {
        _date = _mode switch
        {
            KalendsMode.Year => _date.AddYears(direction),
            KalendsMode.Month => _date.AddMonths(direction),
            KalendsMode.Day => _date.AddDays(direction),
            KalendsMode.Agenda => _date.AddDays(direction * 30),
            _ => _date.AddDays(direction * 7)
        };
        Update();
    }

    private void Previous_Click(object sender, RoutedEventArgs e) => Navigate(-1);
    private void Next_Click(object sender, RoutedEventArgs e) => Navigate(1);
    private void Today_Click(object sender, RoutedEventArgs e) { _date = DateTime.Today; Update(true); }

    private void View_Changed(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        if (_ready && sender.SelectedItem?.Tag is string mode)
        {
            _mode = Enum.Parse<KalendsMode>(mode);
            Update(true);
        }
    }
}
