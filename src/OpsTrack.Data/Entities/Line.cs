namespace OpsTrack.Data.Entities;

/// <summary>A production line, e.g. SMT-2.</summary>
public class Line
{
    public int Id { get; set; }

    public string Code { get; set; }

    public string Name { get; set; }

    public List<Equipment> Equipment { get; set; } = new();
}
