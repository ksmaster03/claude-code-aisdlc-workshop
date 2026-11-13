namespace OpsTrack.Data.Entities;

public enum Priority
{
    P1 = 1,
    P2 = 2,
    P3 = 3
}

public enum WorkRequestStatus
{
    Open = 0,
    InProgress = 1,
    Closed = 2
}

/// <summary>
/// A maintenance work request raised against one piece of equipment.
/// All timestamps are plant local time.
/// </summary>
public class WorkRequest
{
    public int Id { get; set; }

    /// <summary>Running number, format WR-yyMM-nnnn (e.g. WR-2611-0001).</summary>
    public string Number { get; set; }

    public int EquipmentId { get; set; }

    public Equipment Equipment { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public Priority Priority { get; set; }

    public WorkRequestStatus Status { get; set; }

    public string ReportedBy { get; set; }

    public string AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string ClosedBy { get; set; }
}
