using OpsTrack.Business.Shifts;

namespace OpsTrack.Business.Tests;

[Trait("Area", "Shift")]
public class ShiftCalendarTests
{
    private readonly ShiftCalendar _calendar = new();

    [Fact]
    public void Three_am_belongs_to_the_previous_production_day()
    {
        var date = _calendar.GetProductionDate(new DateTime(2026, 11, 12, 3, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 11), date);
    }

    [Fact]
    public void Seven_am_starts_a_new_production_day()
    {
        var date = _calendar.GetProductionDate(new DateTime(2026, 11, 12, 7, 0, 0));

        Assert.Equal(new DateTime(2026, 11, 12), date);
    }

    [Fact]
    public void Ten_am_is_day_shift()
    {
        Assert.Equal("Day", _calendar.GetShift(new DateTime(2026, 11, 12, 10, 0, 0)));
    }
}
