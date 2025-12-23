using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public ServicesController(AppDbContext db) => _db = db;

    private bool IsDesktopClient()
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        return clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
    }

    [HttpGet]
    public async Task<ActionResult<List<Service>>> GetAll([FromQuery] bool activeOnly = false)
    {
        var query = _db.Services.AsQueryable();
        if (activeOnly) query = query.Where(s => s.IsActive);
        return await query.OrderBy(s => s.SortOrder).ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Service>> Get(int id)
    {
        var service = await _db.Services.FindAsync(id);
        return service == null ? NotFound() : Ok(service);
    }

    [HttpPost]
    public async Task<ActionResult<Service>> Create(Service service)
    {
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        if (!IsDesktopClient() && !User.IsInRole("Admin") && !User.IsInRole("Editor"))
        {
            return Forbid();
        }

        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Service service)
    {
        // Для веб-клиентов требуется авторизация
        if (!IsDesktopClient() && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        if (!IsDesktopClient() && !User.IsInRole("Admin") && !User.IsInRole("Editor"))
        {
            return Forbid();
        }

        if (id != service.Id) return BadRequest();
        
        var existing = await _db.Services.FindAsync(id);
        if (existing == null) return NotFound();
        
        existing.Title = service.Title;
        existing.Category = service.Category;
        existing.Description = service.Description;
        existing.Price = service.Price;
        existing.Image = service.Image;
        existing.IsActive = service.IsActive;
        existing.SortOrder = service.SortOrder;
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        // Для веб-клиентов требуется авторизация Admin
        if (!IsDesktopClient())
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return Unauthorized(new { message = "Требуется авторизация" });
            }
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }
        }

        var service = await _db.Services.FindAsync(id);
        if (service == null) return NotFound();
        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
