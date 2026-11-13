using System.Globalization;

namespace OpsTrack.Api.Helpers;

/// <summary>
/// Date helpers for the API layer.
/// </summary>
public static class DateUtil
{
    // production day starts at 08:00
    private const int DayStartHour = 8;
    private const int NightStartHour = 20;

    /// <summary>Production date for a timestamp. Early morning belongs to the day before.</summary>
    public static DateTime GetProductionDate(DateTime dt)
    {
        if (dt.Hour < DayStartHour)
        {
            return dt.Date.AddDays(-1);
        }

        return dt.Date;
    }

    public static string GetShiftName(DateTime dt)
    {
        return dt.Hour >= DayStartHour && dt.Hour < NightStartHour ? "Day" : "Night";
    }

    public static string ToIsoDate(DateTime dt)
    {
        return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
