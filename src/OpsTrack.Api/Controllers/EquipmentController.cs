using Microsoft.AspNetCore.Mvc;
using OpsTrack.Business.Lookups;

namespace OpsTrack.Api.Controllers;

[ApiController]
[Route("api/equipment")]
public class EquipmentController : ControllerBase
{
    private readonly EquipmentService _service;

    public EquipmentController(EquipmentService service)
    {
        _service = service;
    }

    /// <summary>All equipment, optionally for one line (?line=SMT-2).</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<EquipmentDto>>> List([FromQuery] string line, CancellationToken ct)
    {
        return Ok(await _service.ListAsync(line, ct));
    }
}
