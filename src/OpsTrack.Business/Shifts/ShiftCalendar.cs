namespace OpsTrack.Business.Shifts;

/// <summary>
/// Plant shift rules. A production day starts at 07:00, so anything before 07:00
/// belongs to the previous production day (03:00 on the 12th is production day the 11th).
/// Day shift 07:00-19:00, night shift 19:00-07:00.
/// </summary>
public class ShiftCalendar
{
    public static readonly TimeSpan ProductionDayStart = new(7, 0, 0);

    public static readonly TimeSpan NightShiftStart = new(19, 0, 0);

    public DateTime GetProductionDate(DateTime timestamp) =>
        timestamp.TimeOfDay < ProductionDayStart ? timestamp.Date.AddDays(-1) : timestamp.Date;

    public string GetShift(DateTime timestamp)
    {
        var time = timestamp.TimeOfDay;
        return time >= ProductionDayStart && time < NightShiftStart ? "Day" : "Night";
    }
}
