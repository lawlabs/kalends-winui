namespace Kalends;

/// <summary>Lane assignment for overlapping timed events. Intervals are half-open.</summary>
public readonly record struct EventLane(IKalendsEvent Event, int Lane, int LaneCount);

/// <summary>Pure layout math. The same fixtures must produce the same lanes on every platform.</summary>
public static class EventLayoutCalculator
{
    public static IReadOnlyList<EventLane> Layout(IEnumerable<IKalendsEvent> source)
    {
        var result = new List<EventLane>();
        var group = new List<IKalendsEvent>();
        DateTime groupEnd = DateTime.MinValue;

        void Flush()
        {
            var ends = new List<DateTime>();
            var placed = new List<(IKalendsEvent Event, int Lane)>();
            foreach (var item in group)
            {
                int lane = ends.FindIndex(end => end <= item.Start);
                if (lane < 0)
                {
                    lane = ends.Count;
                    ends.Add(item.End);
                }
                else ends[lane] = item.End;
                placed.Add((item, lane));
            }
            result.AddRange(placed.Select(p => new EventLane(p.Event, p.Lane, ends.Count)));
            group.Clear();
        }

        foreach (var item in source.OrderBy(e => e.Start).ThenByDescending(e => e.End))
        {
            if (group.Count > 0 && item.Start >= groupEnd) Flush();
            if (group.Count == 0) groupEnd = item.End;
            group.Add(item);
            if (item.End > groupEnd) groupEnd = item.End;
        }
        Flush();
        return result;
    }

    public static (DateTime Start, DateTime End) Move(IKalendsEvent item, TimeSpan delta, DateTime first, DateTime endExclusive)
    {
        var proposed = item.Start + delta;
        var start = proposed < first ? first : proposed >= endExclusive ? endExclusive.AddMinutes(-15) : proposed;
        return (start, start + (item.End - item.Start));
    }
}
