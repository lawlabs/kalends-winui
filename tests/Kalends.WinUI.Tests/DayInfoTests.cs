namespace Kalends.Tests;

[TestClass]
public sealed class DayInfoTests
{
    [TestMethod]
    public void WorkingTransfer_NeverGetsNonWorkingHighlight_AndKeepsBothMarkers()
    {
        var day = new KalendsDayInfo { Kind = KalendsDayKind.TransferredWorking, IsShortened = true };
        Assert.IsFalse(day.ShouldHighlight(true, true));
        Assert.IsFalse(day.IsNonWorking);
        Assert.AreEqual("W −1h", day.Marker(true, true, true, KalendsStrings.Default));
    }

    [TestMethod]
    public void TogglesAreIndependent_AndDoNotChangeTheDayStatus()
    {
        var holiday = new KalendsDayInfo { Kind = KalendsDayKind.Holiday };
        var weekend = new KalendsDayInfo { Kind = KalendsDayKind.Weekend };
        Assert.IsTrue(holiday.ShouldHighlight(false, true));
        Assert.IsFalse(weekend.ShouldHighlight(false, true));
        Assert.IsTrue(weekend.ShouldHighlight(true, false));
        Assert.AreEqual("", holiday.Marker(true, false, true, KalendsStrings.Default));
        Assert.IsTrue(holiday.IsNonWorking);
    }

    [TestMethod]
    public void WorkingMarkers_CanBeDisabledWithoutHidingTransferredDaysOff()
    {
        Assert.AreEqual("", new KalendsDayInfo { Kind = KalendsDayKind.TransferredWorking, IsShortened = true }
            .Marker(true, true, false, KalendsStrings.Default));
        Assert.AreEqual("↔", new KalendsDayInfo { Kind = KalendsDayKind.TransferredDayOff }
            .Marker(true, true, false, KalendsStrings.Default));
    }
}
