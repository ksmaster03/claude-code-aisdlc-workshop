namespace OpsTrack.Data.Entities;

/// <summary>A machine on a line, e.g. SMT2-RF-01 (reflow oven).</summary>
public class Equipment
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public int LineId { get; set; }

    public Line Line { get; set; }
}
