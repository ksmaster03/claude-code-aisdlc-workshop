using Microsoft.EntityFrameworkCore;
using OpsTrack.Data;

namespace OpsTrack.Business.Lookups;

public class EquipmentDto
{
    public string Code { get; set; }

    public string Name { get; set; }

    public string LineCode { get; set; }

    public string LineName { get; set; }
}

public class EquipmentService
{
    private readonly OpsTrackDbContext _db;

    public EquipmentService(OpsTrackDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<EquipmentDto>> ListAsync(string lineCode, CancellationToken ct = default)
    {
        var query = _db.Equipment.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(lineCode))
        {
            query = query.Where(e => e.Line.Code == lineCode);
        }

        return await query
            .OrderBy(e => e.Code)
            .Select(e => new EquipmentDto
            {
                Code = e.Code,
                Name = e.Name,
                LineCode = e.Line.Code,
                LineName = e.Line.Name,
            })
            .ToListAsync(ct);
    }
}
