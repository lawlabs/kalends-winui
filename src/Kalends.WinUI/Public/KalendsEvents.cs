namespace Kalends;

/// <summary>Values used when the view asks the host to create an event.</summary>
public readonly record struct KalendsEventDraft(DateTime Start, DateTime End, bool IsAllDay);

public sealed class KalendsEventRequestEventArgs : EventArgs
{
    public KalendsEventRequestEventArgs(IKalendsEvent item) => Event = item;
    public IKalendsEvent Event { get; }
}

public sealed class KalendsTimeChangedEventArgs : EventArgs
{
    public KalendsTimeChangedEventArgs(IKalendsEvent item, DateTime start, DateTime end)
    {
        Event = item;
        Start = start;
        End = end;
    }

    public IKalendsEvent Event { get; }
    public DateTime Start { get; }
    public DateTime End { get; }
}

public sealed class KalendsNavigateEventArgs : EventArgs
{
    public KalendsNavigateEventArgs(DateTime date, KalendsMode mode)
    {
        Date = date;
        Mode = mode;
    }

    public DateTime Date { get; }
    public KalendsMode Mode { get; }
}
