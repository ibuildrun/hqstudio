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
public class SessionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SessionsController(AppDbContext db) => _db = db;

    /// <summary>
    /// Начать сессию (при входе в приложение)
    /// </summary>
    [HttpPost("start")]
    public async Task<ActionResult<UserSession>> StartSession([FromBody] StartSessionRequest request)
    {
        var userId = GetUserId();
        
        // Закрываем предыдущие сессии с этого устройства
        var oldSessions = await _db.UserSessions
            .Where(s => s.UserId == userId && s.DeviceId == request.DeviceId && s.EndedAt == null)
            .ToListAsync();
        
        foreach (var old in oldSessions)
        {
            old.Status = UserStatus.Offline;
            old.EndedAt = DateTime.UtcNow;
        }

        var session = new UserSession
        {
            UserId = userId,
            DeviceId = request.DeviceId,
            DeviceName = request.DeviceName,
            Status = UserStatus.Online,
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString()
        };

        _db.UserSessions.Add(session);
        await _db.SaveChangesAsync();

        return Ok(session);
    }

    /// <summary>
    /// Heartbeat - обновление статуса (каждые 15-30 сек)
    /// </summary>
    [HttpPost("heartbeat")]
    public async Task<ActionResult<HeartbeatResponse>> Heartbeat([FromBody] HeartbeatRequest request)
    {
        var userId = GetUserId();
        
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId);

        if (session == null)
            return NotFound(new { message = "Сессия не найдена" });

        session.LastHeartbeat = DateTime.UtcNow;
        session.Status = UserStatus.Online;
        await _db.SaveChangesAsync();

        // Возвращаем количество ожидающих синхронизации данных
        var pendingCallbacks = await _db.CallbackRequests
            .CountAsync(c => c.Status == RequestStatus.New);

        return Ok(new HeartbeatResponse
        {
            Success = true,
            ServerTime = DateTime.UtcNow,
            PendingSync = pendingCallbacks
        });
    }

    /// <summary>
    /// Завершить сессию (при выходе)
    /// </summary>
    [HttpPost("end")]
    public async Task<IActionResult> EndSession([FromBody] EndSessionRequest request)
    {
        var userId = GetUserId();
        
        var session = await _db.UserSessions
            .FirstOrDefaultAsync(s => s.Id == request.SessionId && s.UserId == userId);

        if (session == null)
            return NotFound();

        session.Status = UserStatus.Offline;
        session.EndedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new { message = "Сессия завершена" });
    }

    /// <summary>
    /// Получить активные сессии всех пользователей (для отображения кто онлайн)
    /// </summary>
    [HttpGet("active")]
    public async Task<ActionResult<List<ActiveUserDto>>> GetActiveSessions()
    {
        var cutoffOnline = DateTime.UtcNow.AddSeconds(-30);
        var cutoffAway = DateTime.UtcNow.AddMinutes(-5);

        // Обновляем статусы на основе heartbeat
        var sessions = await _db.UserSessions
            .Include(s => s.User)
            .Where(s => s.EndedAt == null)
            .ToListAsync();

        foreach (var session in sessions)
        {
            if (session.LastHeartbeat >= cutoffOnline)
                session.Status = UserStatus.Online;
            else if (session.LastHeartbeat >= cutoffAway)
                session.Status = UserStatus.Away;
            else
                session.Status = UserStatus.Disconnected;
        }
        await _db.SaveChangesAsync();

        var activeUsers = sessions
            .GroupBy(s => s.UserId)
            .Select(g => new ActiveUserDto
            {
                UserId = g.Key,
                UserName = g.First().User.Name,
                UserRole = g.First().User.Role.ToString(),
                Status = g.OrderByDescending(s => s.LastHeartbeat).First().Status,
                LastSeen = g.Max(s => s.LastHeartbeat),
                DeviceName = g.OrderByDescending(s => s.LastHeartbeat).First().DeviceName
            })
            .OrderByDescending(u => u.Status == UserStatus.Online)
            .ThenByDescending(u => u.LastSeen)
            .ToList();

        return Ok(activeUsers);
    }

    private int GetUserId() => 
        int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
}

public record StartSessionRequest(string DeviceId, string DeviceName);
public record HeartbeatRequest(int SessionId);
public record EndSessionRequest(int SessionId);

public class HeartbeatResponse
{
    public bool Success { get; set; }
    public DateTime ServerTime { get; set; }
    public int PendingSync { get; set; }
}

public class ActiveUserDto
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserRole { get; set; } = string.Empty;
    public UserStatus Status { get; set; }
    public DateTime LastSeen { get; set; }
    public string DeviceName { get; set; } = string.Empty;
}
