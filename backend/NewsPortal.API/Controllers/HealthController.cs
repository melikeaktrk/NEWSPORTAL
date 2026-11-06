using Microsoft.AspNetCore.Mvc;

namespace NewsPortal.API.Controllers;

[ApiController]
[Route("api/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        success = true,
        message = "NewsPortal API çalışıyor.",
        data = new { status = "healthy", timestamp = DateTimeOffset.UtcNow }
    });
}
