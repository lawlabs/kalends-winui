namespace Kalends.Tests;

[TestClass]
public sealed class TimeSelectionTests
{
    private static readonly DateTime Day = new(2026, 9, 24);
    private const double HourHeight = 60;

    [TestMethod]
    public void DragDownKeepsPressTimeAsStart()
    {
        var anchor = Day.AddMinutes(TimeSelection.SnapMinute(9 * HourHeight, HourHeight, 15, true));
        var range = TimeSelection.Range(anchor, 9 * HourHeight, 11 * HourHeight, HourHeight, 15);

        Assert.AreEqual(Day.AddHours(9), range.Start);
        Assert.AreEqual(Day.AddHours(11), range.End);
    }

    [TestMethod]
    public void DragUpKeepsPressTimeAsEnd()
    {
        var anchor = Day.AddMinutes(TimeSelection.SnapMinute(9 * HourHeight, HourHeight, 15, true));
        var range = TimeSelection.Range(anchor, 9 * HourHeight, 8 * HourHeight, HourHeight, 15);

        Assert.AreEqual(Day.AddHours(8), range.Start);
        Assert.AreEqual(Day.AddHours(9), range.End);
    }

    [TestMethod]
    public void ShortDragUsesMinimumSlotInItsDirection()
    {
        var anchor = Day.AddHours(9);
        var up = TimeSelection.Range(anchor, 9 * HourHeight, 9 * HourHeight - 6, HourHeight, 15);
        var down = TimeSelection.Range(anchor, 9 * HourHeight, 9 * HourHeight + 6, HourHeight, 15);

        Assert.AreEqual((Day.AddHours(8).AddMinutes(45), anchor), up);
        Assert.AreEqual((anchor, Day.AddHours(9).AddMinutes(15)), down);
    }

    [TestMethod]
    public void SelectionClampsAtDayBoundaries()
    {
        var midnight = Day.AddMinutes(TimeSelection.SnapMinute(-20, HourHeight, 15, true));
        var lastSlot = Day.AddMinutes(TimeSelection.SnapMinute(25 * HourHeight, HourHeight, 15, true));
        var up = TimeSelection.Range(midnight, 0, -20, HourHeight, 15);
        var down = TimeSelection.Range(lastSlot, 23.75 * HourHeight, 25 * HourHeight, HourHeight, 15);

        Assert.AreEqual((Day, Day.AddMinutes(15)), up);
        Assert.AreEqual((Day.AddHours(23).AddMinutes(45), Day.AddDays(1)), down);
    }
}
