using Microsoft.AspNetCore.Mvc;
using OpsTrack.Api.Helpers;

namespace OpsTrack.Api.Controllers;

/// <summary>Small info endpoint used by the page header.</summary>
[ApiController]
[Route("api/meta")]
public class MetaController : ControllerBase
{
    private readonly IWebHostEnvironment _env;

    public MetaController(IWebHostEnvironment env)
    {
        _env = env;
    }

    [HttpGet]
    public IActionResult Get()
    {
        var now = DateTime.Now;
        return Ok(new
        {
            productionDate = DateUtil.ToIsoDate(DateUtil.GetProductionDate(now)),
            shift = DateUtil.GetShiftName(now),
            environment = _env.EnvironmentName,
        });
    }
}
