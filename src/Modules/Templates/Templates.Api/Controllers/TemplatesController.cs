using Microsoft.AspNetCore.Mvc;

namespace Templates.Api.Controllers;

[ApiController]
[Route("api/templates")]
public sealed class TemplatesController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Module = "Templates",
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
