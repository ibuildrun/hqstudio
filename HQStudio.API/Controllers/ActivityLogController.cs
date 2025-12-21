using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;
using System.Security.Claims;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ActivityLogController : ControllerBase
{
    private readonly AppDbContext _db;

    public ActivityLogController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Получить журнал ответственности
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? source = null,
        [FromQuery] int? userId = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        var query = _db.ActivityLogs.AsQueryable();

        if (!string.IsNullOrEmpty(source))
            query = query.Where(a => a.Source == source);

        if (userId.HasValue)
            query = query.Where(a => a.UserId == userId.Value);

        if (from.HasValue)
            query = query.Where(a => a.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(a => a.CreatedAt <= to.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.UserId,
                a.UserName,
                a.Action,
                a.EntityType,
                a.EntityId,
                a.Details,
                a.Source,
                a.IpAddress,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            items,
            total,
            page,
            pageSize,
            totalPages = (int)Math.Ceiling(total / (double)pageSize)
        });
    }

    /// <summary>
    /// Добавить запись в журнал
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateActivityLogDto dto)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";

        if (!int.TryParse(userIdClaim, out var userId))
            return Unauthorized();

        var log = new ActivityLog
        {
            UserId = userId,
            UserName = userName,
            Action = dto.Action,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            Details = dto.Details,
            Source = dto.Source ?? GetSource(),
            IpAddress = GetIpAddress(),
            CreatedAt = DateTime.UtcNow
        };

        _db.ActivityLogs.Add(log);
        await _db.SaveChangesAsync();

        return Ok(new { log.Id, log.CreatedAt });
    }

    /// <summary>
    /// Получить статистику по журналу
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);

        var stats = new
        {
            TotalToday = await _db.ActivityLogs.CountAsync(a => a.CreatedAt >= today),
            TotalWeek = await _db.ActivityLogs.CountAsync(a => a.CreatedAt >= weekAgo),
            TotalAll = await _db.ActivityLogs.CountAsync(),
            BySource = await _db.ActivityLogs
                .GroupBy(a => a.Source)
                .Select(g => new { Source = g.Key, Count = g.Count() })
                .ToListAsync(),
            ByUser = await _db.ActivityLogs
                .Where(a => a.CreatedAt >= weekAgo)
                .GroupBy(a => new { a.UserId, a.UserName })
                .Select(g => new { g.Key.UserId, g.Key.UserName, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToListAsync()
        };

        return Ok(stats);
    }

    private string GetSource()
    {
        var userAgent = Request.Headers.UserAgent.ToString().ToLower();
        if (userAgent.Contains("hqstudio-desktop"))
            return "Desktop";
        if (userAgent.Contains("mozilla") || userAgent.Contains("chrome"))
            return "Web";
        return "API";
    }

    private string? GetIpAddress()
    {
        return HttpContext.Connection.RemoteIpAddress?.ToString();
    }
}

public class CreateActivityLogDto
{
    public string Action { get; set; } = "";
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? Source { get; set; }
}
