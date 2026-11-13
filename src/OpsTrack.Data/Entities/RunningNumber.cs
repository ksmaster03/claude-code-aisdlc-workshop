namespace OpsTrack.Data.Entities;

/// <summary>
/// Counter table for document numbers. Key is the number prefix, e.g. "WR-2611".
/// Value is the last number issued for that prefix.
/// </summary>
public class RunningNumber
{
    public string Key { get; set; }

    public int Value { get; set; }
}
