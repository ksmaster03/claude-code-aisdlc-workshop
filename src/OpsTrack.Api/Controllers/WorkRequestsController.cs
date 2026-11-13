using Microsoft.AspNetCore.Mvc;
using OpsTrack.Api.Security;
using OpsTrack.Business.WorkRequests;

namespace OpsTrack.Api.Controllers;

[ApiController]
[Route("api/work-requests")]
public class WorkRequestsController : ControllerBase
{
    private readonly WorkRequestService _service;

    public WorkRequestsController(WorkRequestService service)
    {
        _service = service;
    }

    /// <summary>List work requests. Filters: status, priority, line, equipmentCode, page, pageSize (max 500).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<WorkRequestDto>>> List([FromQuery] WorkRequestFilter filter, CancellationToken ct)
    {
        return Ok(await _service.ListAsync(filter, ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<WorkRequestDto>> Get(int id, CancellationToken ct)
    {
        return Ok(await _service.GetAsync(id, ct));
    }

    /// <summary>Any signed-in role can report a problem.</summary>
    [HttpPost]
    [RequireRole(LabRoles.Operator, LabRoles.Technician, LabRoles.Supervisor)]
    public async Task<ActionResult<WorkRequestDto>> Create([FromBody] CreateWorkRequestRequest request, CancellationToken ct)
    {
        var user = HttpContext.GetLabUser();
        var created = await _service.CreateAsync(request, user.Name, ct);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/assign")]
    [RequireRole(LabRoles.Technician, LabRoles.Supervisor)]
    public async Task<ActionResult<WorkRequestDto>> Assign(int id, [FromBody] AssignWorkRequestRequest request, CancellationToken ct)
    {
        var user = HttpContext.GetLabUser();
        var assignee = string.IsNullOrWhiteSpace(request?.AssignedTo) ? user.Name : request.AssignedTo;
        return Ok(await _service.AssignAsync(id, assignee, user.Name, ct));
    }

    [HttpPost("{id:int}/close")]
    [RequireRole(LabRoles.Technician, LabRoles.Supervisor)]
    public async Task<ActionResult<WorkRequestDto>> Close(int id, CancellationToken ct)
    {
        var user = HttpContext.GetLabUser();
        return Ok(await _service.CloseAsync(id, user.Name, ct));
    }
}
