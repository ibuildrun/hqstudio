using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;
using System.Security.Claims;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CallbacksController : ControllerBase
{
    private readonly AppDbContext _db;

    public CallbacksController(AppDbContext db) => _db = db;

    // Public endpoint for website form - с rate limiting для защиты от спама
    [HttpPost]
    [EnableRateLimiting("public-forms")]
    public async Task<ActionResult<CallbackRequest>> Create(CreateCallbackRequest request)
    {
        // Валидация входных данных
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 100)
            return BadRequest(new { message = "Некорректное имя" });
        if (string.IsNullOrWhiteSpace(request.Phone) || request.Phone.Length > 20)
            return BadRequest(new { message = "Некорректный телефон" });
        if (request.Message?.Length > 1000)
            return BadRequest(new { message = "Сообщение слишком длинное" });

        var callback = new CallbackRequest
        {
            Name = request.Name.Trim(),
            Phone = request.Phone.Trim(),
            CarModel = request.CarModel?.Trim(),
            LicensePlate = request.LicensePlate?.Trim()?.ToUpper(),
            Message = request.Message?.Trim(),
            Source = RequestSource.Website, // Всегда Website для публичного эндпоинта
            SourceDetails = request.SourceDetails?.Trim()
        };

        _db.CallbackRequests.Add(callback);
        await _db.SaveChangesAsync();

        return Ok(new { message = "Заявка принята! Мы свяжемся с вами в ближайшее время.", id = callback.Id });
    }

    // Create from desktop app (with auth)
    [HttpPost("manual")]
    [Authorize]
    public async Task<ActionResult<CallbackRequest>> CreateManual(CreateCallbackRequest request)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        
        var callback = new CallbackRequest
        {
            Name = request.Name,
            Phone = request.Phone,
            CarModel = request.CarModel,
            LicensePlate = request.LicensePlate,
            Message = request.Message,
            Source = request.Source ?? RequestSource.WalkIn,
            SourceDetails = request.SourceDetails,
            AssignedUserId = userId
        };

        _db.CallbackRequests.Add(callback);
        await _db.SaveChangesAsync();

        return Ok(callback);
    }

    [HttpGet]
    public async Task<ActionResult<List<CallbackRequest>>> GetAll(
        [FromQuery] RequestStatus? status = null,
        [FromQuery] RequestSource? source = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? limit = null)
    {
        // Проверяем тип клиента
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        // Для веб-клиентов требуется авторизация
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var query = _db.CallbackRequests.AsQueryable();
        
        if (status.HasValue) query = query.Where(c => c.Status == status);
        if (source.HasValue) query = query.Where(c => c.Source == source);
        if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);
        
        query = query.OrderByDescending(c => c.CreatedAt);
        
        // Веб-клиенты получают максимум 20 записей
        // Десктоп может запросить больше или все
        if (!isDesktopClient)
        {
            query = query.Take(20);
        }
        else if (limit.HasValue && limit > 0)
        {
            query = query.Take(limit.Value);
        }
        
        return await query.ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CallbackRequest>> Get(int id)
    {
        // Проверяем тип клиента
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        // Для веб-клиентов требуется авторизация
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var callback = await _db.CallbackRequests.FindAsync(id);
        if (callback == null) return NotFound();
        return Ok(callback);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateCallbackRequest request)
    {
        // Проверяем тип клиента
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        // Для веб-клиентов требуется авторизация
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var callback = await _db.CallbackRequests.FindAsync(id);
        if (callback == null) return NotFound();

        var userId = User.Identity?.IsAuthenticated == true 
            ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")
            : 0;

        if (request.Name != null) callback.Name = request.Name;
        if (request.Phone != null) callback.Phone = request.Phone;
        if (request.CarModel != null) callback.CarModel = request.CarModel;
        if (request.LicensePlate != null) callback.LicensePlate = request.LicensePlate;
        if (request.Message != null) callback.Message = request.Message;
        if (request.Source.HasValue) callback.Source = request.Source.Value;
        if (request.SourceDetails != null) callback.SourceDetails = request.SourceDetails;
        
        if (request.Status.HasValue)
        {
            var oldStatus = callback.Status;
            callback.Status = request.Status.Value;
            
            if (oldStatus == RequestStatus.New && request.Status == RequestStatus.Processing)
            {
                callback.ProcessedAt = DateTime.UtcNow;
                if (userId > 0) callback.AssignedUserId = userId;
            }
            else if (request.Status == RequestStatus.Completed)
            {
                callback.CompletedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        return Ok(callback);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] RequestStatus status)
    {
        // Проверяем тип клиента
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        // Для веб-клиентов требуется авторизация
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var callback = await _db.CallbackRequests.FindAsync(id);
        if (callback == null) return NotFound();
        
        var userId = User.Identity?.IsAuthenticated == true 
            ? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0")
            : 0;
        var oldStatus = callback.Status;
        callback.Status = status;
        
        if (oldStatus == RequestStatus.New && status == RequestStatus.Processing)
        {
            callback.ProcessedAt = DateTime.UtcNow;
            if (userId > 0) callback.AssignedUserId = userId;
        }
        else if (status == RequestStatus.Completed)
        {
            callback.CompletedAt = DateTime.UtcNow;
        }
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpGet("stats")]
    public async Task<ActionResult<CallbackStats>> GetStats()
    {
        // Проверяем тип клиента
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        // Для веб-клиентов требуется авторизация
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var today = DateTime.UtcNow.Date;
        var weekAgo = today.AddDays(-7);
        var monthAgo = today.AddMonths(-1);

        var stats = new CallbackStats
        {
            TotalNew = await _db.CallbackRequests.CountAsync(c => c.Status == RequestStatus.New),
            TotalProcessing = await _db.CallbackRequests.CountAsync(c => c.Status == RequestStatus.Processing),
            TotalCompleted = await _db.CallbackRequests.CountAsync(c => c.Status == RequestStatus.Completed),
            TodayCount = await _db.CallbackRequests.CountAsync(c => c.CreatedAt >= today),
            WeekCount = await _db.CallbackRequests.CountAsync(c => c.CreatedAt >= weekAgo),
            MonthCount = await _db.CallbackRequests.CountAsync(c => c.CreatedAt >= monthAgo),
            BySource = await _db.CallbackRequests
                .GroupBy(c => c.Source)
                .Select(g => new SourceStat { Source = g.Key, Count = g.Count() })
                .ToListAsync()
        };

        return Ok(stats);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Проверяем тип клиента
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        // Для веб-клиентов требуется авторизация с ролью Admin или Manager
        if (!isDesktopClient)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized(new { message = "Требуется авторизация" });
            }
            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                return Forbid();
            }
        }
        
        var callback = await _db.CallbackRequests.FindAsync(id);
        if (callback == null) return NotFound();
        _db.CallbackRequests.Remove(callback);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateCallbackRequest(
    string Name, 
    string Phone, 
    string? CarModel, 
    string? LicensePlate,
    string? Message,
    RequestSource? Source,
    string? SourceDetails
);

public record UpdateCallbackRequest(
    string? Name,
    string? Phone,
    string? CarModel,
    string? LicensePlate,
    string? Message,
    RequestStatus? Status,
    RequestSource? Source,
    string? SourceDetails
);

public class CallbackStats
{
    public int TotalNew { get; set; }
    public int TotalProcessing { get; set; }
    public int TotalCompleted { get; set; }
    public int TodayCount { get; set; }
    public int WeekCount { get; set; }
    public int MonthCount { get; set; }
    public List<SourceStat> BySource { get; set; } = new();
}

public class SourceStat
{
    public RequestSource Source { get; set; }
    public int Count { get; set; }
}
