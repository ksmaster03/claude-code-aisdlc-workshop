using System.Globalization;
using Microsoft.EntityFrameworkCore;
using OpsTrack.Data;

namespace OpsTrack.Business.Numbering;

/// <summary>
/// Issues work request numbers in the format WR-yyMM-nnnn. The counter restarts every month.
/// </summary>
public class RunningNumberService
{
    private readonly OpsTrackDbContext _db;

    public RunningNumberService(OpsTrackDbContext db)
    {
        _db = db;
    }

    public async Task<string> NextAsync(DateTime now, CancellationToken ct = default)
    {
        var prefix = "WR-" + now.ToString("yyMM", CultureInfo.InvariantCulture) + "-";

        var last = await _db.WorkRequests
            .Where(w => w.Number.StartsWith(prefix))
            .Select(w => w.Number)
            .MaxAsync(ct);

        var next = 1;
        if (last != null)
        {
            next = int.Parse(last.Substring(prefix.Length), CultureInfo.InvariantCulture) + 1;
        }

        return prefix + next.ToString("0000", CultureInfo.InvariantCulture);
    }
}
