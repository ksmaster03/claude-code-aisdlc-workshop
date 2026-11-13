using OpsTrack.Data.Entities;

namespace OpsTrack.Business.WorkRequests;

public class CreateWorkRequestRequest
{
    public string EquipmentCode { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    /// <summary>Defaults to P3 when not given.</summary>
    public Priority? Priority { get; set; }
}

public class AssignWorkRequestRequest
{
    /// <summary>User name of the technician. Empty means "assign to me".</summary>
    public string AssignedTo { get; set; }
}

public class WorkRequestFilter
{
    public WorkRequestStatus? Status { get; set; }

    public Priority? Priority { get; set; }

    /// <summary>Line code, e.g. SMT-2.</summary>
    public string Line { get; set; }

    public string EquipmentCode { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 50;
}

public class WorkRequestDto
{
    public int Id { get; set; }

    public string Number { get; set; }

    public string EquipmentCode { get; set; }

    public string EquipmentName { get; set; }

    public string LineCode { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public Priority Priority { get; set; }

    public WorkRequestStatus Status { get; set; }

    public string ReportedBy { get; set; }

    public string AssignedTo { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>Production date (yyyy-MM-dd) the request was raised in, from ShiftCalendar.</summary>
    public string ProductionDate { get; set; }

    public string Shift { get; set; }

    public DateTime DueAt { get; set; }

    public bool IsOverdue { get; set; }

    public DateTime? ClosedAt { get; set; }

    public string ClosedBy { get; set; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; }

    public int Total { get; set; }

    public int Page { get; set; }

    public int PageSize { get; set; }
}
