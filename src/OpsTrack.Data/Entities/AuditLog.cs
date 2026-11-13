namespace OpsTrack.Data.Entities;

public class AuditLog
{
    public long Id { get; set; }

    /// <summary>Entity name, e.g. "WorkRequest".</summary>
    public string Entity { get; set; }

    public string EntityId { get; set; }

    /// <summary>Create, Assign, Close ...</summary>
    public string Action { get; set; }

    public string By { get; set; }

    public DateTime At { get; set; }

    public string Detail { get; set; }
}
