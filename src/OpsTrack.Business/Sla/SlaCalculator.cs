using OpsTrack.Data.Entities;

namespace OpsTrack.Business.Sla;

/// <summary>
/// SLA rules from docs/sla-policy.md: P1 = 4 hours, P2 = 24 hours, P3 = 72 hours,
/// counted in calendar hours from CreatedAt.
/// </summary>
public class SlaCalculator
{
    public TimeSpan GetTarget(Priority priority) => priority switch
    {
        Priority.P1 => TimeSpan.FromHours(4),
        Priority.P2 => TimeSpan.FromHours(24),
        Priority.P3 => TimeSpan.FromHours(72),
        _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, "Unknown priority"),
    };

    public DateTime DueAt(DateTime createdAt, Priority priority) => createdAt.Add(GetTarget(priority));

    /// <summary>
    /// True when <paramref name="now"/> is later than the due time.
    /// Finishing exactly at the due time is still within SLA.
    /// </summary>
    public bool IsBreached(DateTime createdAt, DateTime now, Priority priority) => now > DueAt(createdAt, priority);
}
