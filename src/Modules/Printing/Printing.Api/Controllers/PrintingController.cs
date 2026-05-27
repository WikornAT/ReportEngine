using Microsoft.AspNetCore.Mvc;

namespace Printing.Api.Controllers;

[ApiController]
[Route("api/printing")]
public sealed class PrintingController : ControllerBase
{
    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            Module = "Printing",
            Status = "ok",
            Timestamp = DateTimeOffset.UtcNow
        });
    }
}
