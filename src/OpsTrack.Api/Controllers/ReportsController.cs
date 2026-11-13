using Microsoft.AspNetCore.Mvc;
using OpsTrack.Business.Reports;

namespace OpsTrack.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly ReportService _service;

    public ReportsController(ReportService service)
    {
        _service = service;
    }

    /// <summary>Work request summary by line. from and to are yyyy-MM-dd, both inclusive.</summary>
    [HttpGet("summary")]
    public async Task<ActionResult<IReadOnlyList<LineSummaryDto>>> Summary([FromQuery] string from, [FromQuery] string to, CancellationToken ct)
    {
        return Ok(await _service.GetSummaryAsync(from, to, ct));
    }
}
