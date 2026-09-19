using System.Globalization;
using System.Text.Json;

namespace Kalends.Tests;

[TestClass]
public sealed class EventLayoutTests
{
    private static readonly DateTime Date = new(2026, 9, 19);

    [TestMethod]
    public void WeekStartsOnConfiguredFirstDay()
    {
        Assert.AreEqual(new DateTime(2026, 9, 14), KalendsDate.StartOfWeek(Date, DayOfWeek.Monday));
        Assert.AreEqual(new DateTime(2026, 9, 13), KalendsDate.StartOfWeek(Date, DayOfWeek.Sunday));
        Assert.AreEqual(new DateTime(2026, 12, 28), KalendsDate.StartOfWeek(new DateTime(2027, 1, 1), DayOfWeek.Monday));
    }

    [TestMethod]
    public void MonthGridStartsOnFirstWeekday()
    {
        Assert.AreEqual(new DateTime(2026, 2, 23), KalendsDate.MonthGridStart(new DateTime(2026, 3, 1), DayOfWeek.Monday));
    }

    [TestMethod]
    public void SnapUsesFifteenMinutesBothWays()
    {
        Assert.AreEqual(15, KalendsDate.Snap(8));
        Assert.AreEqual(-15, KalendsDate.Snap(-8));
        Assert.AreEqual(0, KalendsDate.Snap(7));
    }

    [TestMethod]
    public void OvernightEventBelongsToBothDays()
    {
        var overnight = Event("o", Date.AddHours(23), Date.AddHours(25));
        Assert.IsTrue(KalendsDate.Intersects(overnight, Date, Date.AddDays(1)));
        Assert.IsTrue(KalendsDate.Intersects(overnight, Date.AddDays(1), Date.AddDays(2)));
        Assert.IsFalse(KalendsDate.Intersects(Event("m", Date.AddHours(22), Date.AddDays(1)), Date.AddDays(1), Date.AddDays(2)));
    }

    [TestMethod]
    public void MovePreservesDurationAtBoundaries()
    {
        var overnight = Event("o", Date.AddHours(23), Date.AddHours(25));
        var shifted = EventLayoutCalculator.Move(overnight, TimeSpan.FromHours(2), Date, Date.AddDays(7));
        Assert.AreEqual(TimeSpan.FromHours(2), shifted.End - shifted.Start);
        Assert.AreEqual(Date.AddDays(1).AddHours(1), shifted.Start);

        var item = Event("a", Date.AddHours(9), Date.AddHours(10));
        var clamped = EventLayoutCalculator.Move(item, TimeSpan.FromDays(-3), Date, Date.AddDays(7));
        Assert.AreEqual(Date, clamped.Start);
        Assert.AreEqual(TimeSpan.FromHours(1), clamped.End - clamped.Start);

        var last = EventLayoutCalculator.Move(item, TimeSpan.FromDays(9), Date, Date.AddDays(7));
        Assert.AreEqual(Date.AddDays(7).AddMinutes(-15), last.Start);
        Assert.AreEqual(TimeSpan.FromHours(1), last.End - last.Start);
    }

    [TestMethod]
    public void EmptyLayoutIsEmpty() => Assert.IsEmpty(EventLayoutCalculator.Layout([]));

    [TestMethod]
    [DataRow("adjacent-events.json")]
    [DataRow("overlap-group.json")]
    [DataRow("triple-overlap.json")]
    public void FixtureLanesMatch(string fileName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
        var document = JsonDocument.Parse(File.ReadAllText(path)).RootElement;
        var events = document.GetProperty("events").EnumerateArray().Select(node => Event(
            node.GetProperty("id").GetString()!,
            DateTime.Parse(node.GetProperty("start").GetString()!, CultureInfo.InvariantCulture),
            DateTime.Parse(node.GetProperty("end").GetString()!, CultureInfo.InvariantCulture))).ToArray();
        var layout = EventLayoutCalculator.Layout(events);
        foreach (var expected in document.GetProperty("expected").EnumerateArray())
        {
            var id = expected.GetProperty("id").GetString();
            var placed = layout.Single(item => item.Event.Id == id);
            Assert.AreEqual(expected.GetProperty("lane").GetInt32(), placed.Lane, id);
            Assert.AreEqual(expected.GetProperty("laneCount").GetInt32(), placed.LaneCount, id);
        }
    }

    [TestMethod]
    public void AdjacentEventsReuseAFreeLane()
    {
        var a = Event("a", Date.AddHours(9), Date.AddHours(10));
        var b = Event("b", Date.AddHours(10), Date.AddHours(11));
        var c = Event("c", Date.AddHours(9.5), Date.AddHours(10.5));
        var overlap = EventLayoutCalculator.Layout([a, b, c]);
        Assert.AreEqual(overlap.Single(x => x.Event == a).Lane, overlap.Single(x => x.Event == b).Lane);
    }

    private static KalendsEvent Event(string id, DateTime start, DateTime end) =>
        new() { Id = id, Title = id, Start = start, End = end };
}
