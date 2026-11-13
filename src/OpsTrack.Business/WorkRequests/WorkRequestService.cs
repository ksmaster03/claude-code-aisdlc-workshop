using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsTrack.Business.Common;
using OpsTrack.Business.Notifications;
using OpsTrack.Business.Numbering;
using OpsTrack.Business.Shifts;
using OpsTrack.Business.Sla;
using OpsTrack.Data;
using OpsTrack.Data.Entities;

namespace OpsTrack.Business.WorkRequests;

public class WorkRequestService
{
    public const int MaxPageSize = 500;

    private readonly OpsTrackDbContext _db;
    private readonly RunningNumberService _numbers;
    private readonly SlaCalculator _sla;
    private readonly ShiftCalendar _shifts;
    private readonly INotificationClient _notifications;
    private readonly IClock _clock;
    private readonly ILogger<WorkRequestService> _logger;

    public WorkRequestService(
        OpsTrackDbContext db,
        RunningNumberService numbers,
        SlaCalculator sla,
        ShiftCalendar shifts,
        INotificationClient notifications,
        IClock clock,
        ILogger<WorkRequestService> logger)
    {
        _db = db;
        _numbers = numbers;
        _sla = sla;
        _shifts = shifts;
        _notifications = notifications;
        _clock = clock;
        _logger = logger;
    }

    public async Task<WorkRequestDto> CreateAsync(CreateWorkRequestRequest request, string reportedBy, CancellationToken ct = default)
    {
        Guard.NotNull(request, nameof(request));
        var title = Guard.NotEmpty(request.Title, "title");
        Guard.NotEmpty(reportedBy, nameof(reportedBy));

        var equipment = await _db.Equipment
            .Include(e => e.Line)
            .FirstOrDefaultAsync(e => e.Code == request.EquipmentCode, ct);

        var now = _clock.Now;
        var number = await _numbers.NextAsync(now, ct);

        var workRequest = new WorkRequest
        {
            Number = number,
            EquipmentId = equipment.Id,
            Equipment = equipment,
            Title = title,
            Description = request.Description?.Trim(),
            Priority = request.Priority ?? Priority.P3,
            Status = WorkRequestStatus.Open,
            ReportedBy = reportedBy,
            CreatedAt = now,
        };

        _db.WorkRequests.Add(workRequest);
        _db.AuditLogs.Add(NewAudit(workRequest.Number, "Create", reportedBy, "Priority=" + workRequest.Priority));
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Work request {Number} created by {User} for {Equipment}", number, reportedBy, equipment.Code);
        return ToDto(workRequest);
    }

    public async Task<WorkRequestDto> GetAsync(int id, CancellationToken ct = default)
    {
        Guard.Positive(id, nameof(id));

        var workRequest = await _db.WorkRequests
            .AsNoTracking()
            .Include(w => w.Equipment).ThenInclude(e => e.Line)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (workRequest == null)
        {
            throw new NotFoundException($"ไม่พบงานซ่อม id {id}");
        }

        return ToDto(workRequest);
    }

    public async Task<PagedResult<WorkRequestDto>> ListAsync(WorkRequestFilter filter, CancellationToken ct = default)
    {
        filter ??= new WorkRequestFilter();
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = Math.Clamp(filter.PageSize, 1, MaxPageSize);

        var query = _db.WorkRequests
            .AsNoTracking()
            .Include(w => w.Equipment).ThenInclude(e => e.Line)
            .AsQueryable();

        if (filter.Status != null)
        {
            query = query.Where(w => w.Status == filter.Status);
        }

        if (filter.Priority != null)
        {
            query = query.Where(w => w.Priority == filter.Priority);
        }

        if (!string.IsNullOrWhiteSpace(filter.Line))
        {
            query = query.Where(w => w.Equipment.Line.Code == filter.Line);
        }

        if (!string.IsNullOrWhiteSpace(filter.EquipmentCode))
        {
            query = query.Where(w => w.Equipment.Code == filter.EquipmentCode);
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<WorkRequestDto>
        {
            Items = rows.Select(ToDto).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize,
        };
    }

    public async Task<WorkRequestDto> AssignAsync(int id, string assignedTo, string by, CancellationToken ct = default)
    {
        Guard.Positive(id, nameof(id));
        assignedTo = Guard.NotEmpty(assignedTo, nameof(assignedTo));

        var workRequest = await _db.WorkRequests
            .Include(w => w.Equipment).ThenInclude(e => e.Line)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (workRequest == null)
        {
            throw new NotFoundException($"ไม่พบงานซ่อม id {id}");
        }

        if (workRequest.Status == WorkRequestStatus.Closed)
        {
            throw new ConflictException($"งาน {workRequest.Number} ปิดไปแล้ว มอบหมายไม่ได้");
        }

        workRequest.AssignedTo = assignedTo;
        workRequest.Status = WorkRequestStatus.InProgress;
        _db.AuditLogs.Add(NewAudit(workRequest.Number, "Assign", by, "AssignedTo=" + assignedTo));
        await _db.SaveChangesAsync(ct);

        return ToDto(workRequest);
    }

    public async Task<WorkRequestDto> CloseAsync(int id, string closedBy, CancellationToken ct = default)
    {
        Guard.Positive(id, nameof(id));
        Guard.NotEmpty(closedBy, nameof(closedBy));

        var workRequest = await _db.WorkRequests
            .Include(w => w.Equipment).ThenInclude(e => e.Line)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

        if (workRequest == null)
        {
            throw new NotFoundException($"ไม่พบงานซ่อม id {id}");
        }

        if (workRequest.Status == WorkRequestStatus.Closed)
        {
            throw new ConflictException($"งาน {workRequest.Number} ปิดไปแล้ว");
        }

        workRequest.Status = WorkRequestStatus.Closed;
        workRequest.ClosedAt = _clock.Now;
        workRequest.ClosedBy = closedBy;
        await _db.SaveChangesAsync(ct);

        _db.AuditLogs.Add(NewAudit(workRequest.Number, "Close", closedBy, null));
        await _db.SaveChangesAsync(ct);

        if (_sla.IsBreached(workRequest.CreatedAt, workRequest.ClosedAt.Value, workRequest.Priority))
        {
            await _notifications.SendSlaBreachAsync(
                new SlaBreachNotification
                {
                    Number = workRequest.Number,
                    Title = workRequest.Title,
                    LineCode = workRequest.Equipment.Line.Code,
                    EquipmentCode = workRequest.Equipment.Code,
                    Priority = workRequest.Priority.ToString(),
                    CreatedAt = workRequest.CreatedAt,
                    DueAt = _sla.DueAt(workRequest.CreatedAt, workRequest.Priority),
                    ClosedAt = workRequest.ClosedAt,
                    ClosedBy = closedBy,
                },
                ct);
        }

        _logger.LogInformation("Work request {Number} closed by {User}", workRequest.Number, closedBy);
        return ToDto(workRequest);
    }

    private AuditLog NewAudit(string number, string action, string by, string detail) => new()
    {
        Entity = nameof(WorkRequest),
        EntityId = number,
        Action = action,
        By = by,
        At = _clock.Now,
        Detail = detail,
    };

    private WorkRequestDto ToDto(WorkRequest w) => new()
    {
        Id = w.Id,
        Number = w.Number,
        EquipmentCode = w.Equipment?.Code,
        EquipmentName = w.Equipment?.Name,
        LineCode = w.Equipment?.Line?.Code,
        Title = w.Title,
        Description = w.Description,
        Priority = w.Priority,
        Status = w.Status,
        ReportedBy = w.ReportedBy,
        AssignedTo = w.AssignedTo,
        CreatedAt = w.CreatedAt,
        ProductionDate = _shifts.GetProductionDate(w.CreatedAt).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        Shift = _shifts.GetShift(w.CreatedAt),
        DueAt = _sla.DueAt(w.CreatedAt, w.Priority),
        IsOverdue = w.Status != WorkRequestStatus.Closed
            ? _sla.IsBreached(w.CreatedAt, _clock.Now, w.Priority)
            : _sla.IsBreached(w.CreatedAt, w.ClosedAt ?? _clock.Now, w.Priority),
        ClosedAt = w.ClosedAt,
        ClosedBy = w.ClosedBy,
    };
}
