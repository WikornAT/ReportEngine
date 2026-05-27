using Microsoft.AspNetCore.Mvc;

namespace Scheduling.Api.Controllers;

[ApiController]
[Route("api/scheduling")]
public sealed class SchedulingController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Module = "Scheduling",
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
