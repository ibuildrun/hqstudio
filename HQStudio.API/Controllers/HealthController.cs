using Microsoft.AspNetCore.Mvc;

namespace HQStudio.API.Controllers;

/// <summary>
/// Health check endpoint для мониторинга состояния API
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    /// <summary>
    /// Проверка доступности API
    /// </summary>
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        });
    }

    /// <summary>
    /// Детальная проверка состояния
    /// </summary>
    [HttpGet("detailed")]
    public IActionResult GetDetailed()
    {
        return Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow,
            version = "1.0.0",
            uptime = Environment.TickCount64 / 1000,
            memory = new
            {
                workingSet = Environment.WorkingSet / 1024 / 1024,
                unit = "MB"
            }
        });
    }
}
