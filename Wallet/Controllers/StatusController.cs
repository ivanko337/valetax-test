using Microsoft.AspNetCore.Mvc;

namespace Wallet.Controllers;

[ApiController]
[Route("")]
public sealed class StatusController : ControllerBase
{
    [HttpGet]
    public IActionResult GetStatus()
    {
        return Ok(new { service = "Wallet", status = "Running" });
    }

    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "Healthy" });
    }
}
