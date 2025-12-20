using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SubscriptionsController(AppDbContext db) => _db = db;

    // Public endpoint for newsletter signup - с rate limiting для защиты от спама
    [HttpPost]
    [EnableRateLimiting("public-forms")]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        // Валидация email
        if (string.IsNullOrWhiteSpace(request.Email) || 
            request.Email.Length > 100 ||
            !request.Email.Contains('@') ||
            !request.Email.Contains('.'))
        {
            return BadRequest(new { message = "Некорректный email" });
        }

        var email = request.Email.Trim().ToLower();

        if (await _db.Subscriptions.AnyAsync(s => s.Email == email))
            return Ok(new { message = "Вы уже подписаны на рассылку!" });

        _db.Subscriptions.Add(new Subscription { Email = email });
        await _db.SaveChangesAsync();

        return Ok(new { message = "Спасибо за подписку!" });
    }

    [HttpGet]
    [Authorize]
    public async Task<ActionResult<List<Subscription>>> GetAll([FromQuery] int? limit = null)
    {
        var query = _db.Subscriptions.OrderByDescending(s => s.CreatedAt).AsQueryable();
        
        // Ограничение для веб-клиентов
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
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

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var sub = await _db.Subscriptions.FindAsync(id);
        if (sub == null) return NotFound();
        _db.Subscriptions.Remove(sub);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record SubscribeRequest(string Email);
