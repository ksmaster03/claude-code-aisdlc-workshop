using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OpsTrack.Business.Common;
using OpsTrack.Data;
using OpsTrack.Data.Entities;

namespace OpsTrack.Business.Reports;

public class LineSummaryDto
{
    public string LineCode { get; set; }

    public string LineName { get; set; }

    public int Total { get; set; }

    public int Open { get; set; }

    public int InProgress { get; set; }

    public int Closed { get; set; }
}

public class ReportService
{
    private readonly OpsTrackDbContext _db;
    private readonly ILogger<ReportService> _logger;

    public ReportService(OpsTrackDbContext db, ILogger<ReportService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Work request counts per line for requests created between <paramref name="from"/> and
    /// <paramref name="to"/> (both inclusive, format yyyy-MM-dd as sent by the report page).
    /// </summary>
    public async Task<IReadOnlyList<LineSummaryDto>> GetSummaryAsync(string from, string to, CancellationToken ct = default)
    {
        Guard.NotEmpty(from, nameof(from));
        Guard.NotEmpty(to, nameof(to));

        var fromDate = DateTime.Parse(from);
        var toDate = DateTime.Parse(to).AddDays(1);

        _logger.LogInformation("Report summary requested from={From} to={To}", from, to);

        var rows = await _db.WorkRequests
            .AsNoTracking()
            .Where(w => w.CreatedAt >= fromDate && w.CreatedAt < toDate)
            .GroupBy(w => new { w.Equipment.Line.Code, w.Equipment.Line.Name })
            .Select(g => new LineSummaryDto
            {
                LineCode = g.Key.Code,
                LineName = g.Key.Name,
                Total = g.Count(),
                Open = g.Count(x => x.Status == WorkRequestStatus.Open),
                InProgress = g.Count(x => x.Status == WorkRequestStatus.InProgress),
                Closed = g.Count(x => x.Status == WorkRequestStatus.Closed),
            })
            .OrderBy(x => x.LineCode)
            .ToListAsync(ct);

        _logger.LogInformation("Report summary returned {Rows} rows", rows.Count);
        return rows;
    }
}
