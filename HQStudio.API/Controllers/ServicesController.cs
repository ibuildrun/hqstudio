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
    [Authorize(Roles = "Admin,Editor")]
    public async Task<ActionResult<Service>> Create(Service service)
    {
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = service.Id }, service);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Editor")]
    public async Task<IActionResult> Update(int id, Service service)
    {
        if (id != service.Id) return BadRequest();
        _db.Entry(service).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service == null) return NotFound();
        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
