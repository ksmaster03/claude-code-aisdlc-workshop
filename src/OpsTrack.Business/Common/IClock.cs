namespace OpsTrack.Business.Common;

/// <summary>
/// Source of "now". Services must use this instead of DateTime.Now so tests can control time.
/// </summary>
public interface IClock
{
    /// <summary>Plant local time.</summary>
    DateTime Now { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime Now => DateTime.Now;
}
