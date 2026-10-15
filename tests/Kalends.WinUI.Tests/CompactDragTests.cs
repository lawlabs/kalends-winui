namespace Kalends.Tests;

[TestClass]
public sealed class CompactDragTests
{
    [TestMethod]
    public void TimedMovePreservesTimeAndDurationWithoutChangingTheHostEvent()
    {
        var item = new KalendsEvent { Start = new(2026, 10, 12, 9, 30, 0), End = new(2026, 10, 12, 11, 0, 0) };
        var result = CompactDrag.MoveToDate(item, new(2026, 10, 12), new(2026, 10, 20));
        Assert.AreEqual(new DateTime(2026, 10, 20, 9, 30, 0), result.Start);
        Assert.AreEqual(new DateTime(2026, 10, 20, 11, 0, 0), result.End);
        Assert.AreEqual(new DateTime(2026, 10, 12, 9, 30, 0), item.Start);
    }

    [TestMethod]
    public void MovingTheSecondDayOfAnAllDayEventKeepsThatDayUnderThePointer()
    {
        var item = new KalendsEvent { Start = new(2026, 10, 13), End = new(2026, 10, 15), IsAllDay = true };
        var result = CompactDrag.MoveToDate(item, new(2026, 10, 14), new(2026, 10, 21));
        Assert.AreEqual(new DateTime(2026, 10, 20), result.Start);
        Assert.AreEqual(new DateTime(2026, 10, 22), result.End);
    }

    [TestMethod]
    public void OvernightSegmentCanMoveAcrossAMonthBoundary()
    {
        var item = new KalendsEvent { Start = new(2026, 10, 31, 23, 30, 0), End = new(2026, 11, 1, 1, 15, 0) };
        var result = CompactDrag.MoveToDate(item, new(2026, 11, 1), new(2026, 10, 30));
        Assert.AreEqual(new DateTime(2026, 10, 29, 23, 30, 0), result.Start);
        Assert.AreEqual(new DateTime(2026, 10, 30, 1, 15, 0), result.End);
    }

    [TestMethod]
    public void DroppingOnTheGrabbedDateIsANoOp()
    {
        var item = new KalendsEvent { Start = new(2026, 10, 13), End = new(2026, 10, 15), IsAllDay = true };
        Assert.AreEqual((item.Start, item.End), CompactDrag.MoveToDate(item, new(2026, 10, 14), new(2026, 10, 14)));
    }
}
