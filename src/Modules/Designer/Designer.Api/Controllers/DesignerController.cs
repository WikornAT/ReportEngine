using Microsoft.AspNetCore.Mvc;

namespace Designer.Api.Controllers;

[ApiController]
[Route("api/designer")]
public sealed class DesignerController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Module = "Designer",
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
