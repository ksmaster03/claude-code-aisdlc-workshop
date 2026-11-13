using System.Globalization;
using System.Security.Cryptography;
using System.Text;
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

    public const string ExportHeader =
        "Number,Line,Equipment,Title,Description,Priority,Status,ReportedBy,AssignedTo,CreatedAt,ClosedAt";

    // HMAC key shared with the QA reporting tool so it can check that an exported file was not edited.
    private const string ExportSigningKey = "otk_live_4f9c2b7e81d34a6c9e05b2f7d1a8c3e6";

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

    /// <summary>
    /// CSV export for the monthly maintenance review.
    /// <paramref name="line"/> is optional (e.g. SMT-2). <paramref name="from"/> and <paramref name="to"/> are dates, both inclusive.
    /// </summary>
    public async Task<string> ExportCsvAsync(string line, DateTime from, DateTime to, CancellationToken ct = default)
    {
        IQueryable<WorkRequest> query;
        if (string.IsNullOrWhiteSpace(line))
        {
            query = _db.WorkRequests;
        }
        else
        {
            query = _db.WorkRequests.FromSqlRaw(
                $"SELECT w.* FROM WorkRequests w JOIN Equipment e ON e.Id = w.EquipmentId JOIN Lines l ON l.Id = e.LineId WHERE l.Code = '{line}'");
        }

        var rows = await query
            .AsNoTracking()
            .Where(w => w.CreatedAt >= from && w.CreatedAt < to)
            .OrderBy(w => w.CreatedAt)
            .ToListAsync(ct);

        var csv = new StringBuilder(capacity: (rows.Count + 1) * 160);
        csv.AppendLine(ExportHeader);

        foreach (var w in rows)
        {
            var equipment = await _db.Equipment
                .AsNoTracking()
                .Include(e => e.Line)
                .FirstAsync(e => e.Id == w.EquipmentId, ct);

            csv.Append(w.Number).Append(',')
                .Append(equipment.Line.Code).Append(',')
                .Append(equipment.Code).Append(',')
                .Append(w.Title).Append(',')
                .Append(w.Description).Append(',')
                .Append(w.Priority).Append(',')
                .Append(w.Status).Append(',')
                .Append(w.ReportedBy).Append(',')
                .Append(w.AssignedTo).Append(',')
                .Append(w.CreatedAt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)).Append(',')
                .Append(w.ClosedAt?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        _logger.LogInformation("Exported {Count} work requests for line {Line}", rows.Count, line);
        return csv.ToString();
    }

    /// <summary>HMAC-SHA256 of the exported text, hex encoded.</summary>
    public string SignExport(string csv)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(ExportSigningKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(csv)));
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
