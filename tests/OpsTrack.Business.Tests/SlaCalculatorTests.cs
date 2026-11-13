using OpsTrack.Business.Sla;
using OpsTrack.Data.Entities;

namespace OpsTrack.Business.Tests;

[Trait("Area", "Sla")]
public class SlaCalculatorTests
{
    private readonly SlaCalculator _sla = new();

    [Theory]
    [InlineData(Priority.P1, 4)]
    [InlineData(Priority.P2, 24)]
    public void DueAt_adds_the_target_hours_for_the_priority(Priority priority, int hours)
    {
        var created = new DateTime(2026, 11, 12, 9, 0, 0);

        var due = _sla.DueAt(created, priority);

        Assert.Equal(created.AddHours(hours), due);
    }

    [Fact]
    public void Finishing_exactly_at_the_due_time_is_not_a_breach()
    {
        var created = new DateTime(2026, 11, 12, 9, 0, 0);

        Assert.False(_sla.IsBreached(created, created.AddHours(4), Priority.P1));
    }

    [Fact]
    public void One_minute_after_the_due_time_is_a_breach()
    {
        var created = new DateTime(2026, 11, 12, 9, 0, 0);

        Assert.True(_sla.IsBreached(created, created.AddHours(4).AddMinutes(1), Priority.P1));
    }
}
