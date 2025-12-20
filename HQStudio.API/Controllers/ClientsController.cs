using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ClientsController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Client>>> GetAll([FromQuery] int? limit = null)
    {
        var query = _db.Clients.OrderByDescending(c => c.CreatedAt).AsQueryable();
        
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

    [HttpGet("{id}")]
    public async Task<ActionResult<Client>> Get(int id)
    {
        var client = await _db.Clients.Include(c => c.Orders).FirstOrDefaultAsync(c => c.Id == id);
        return client == null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<Client>> Create(Client client)
    {
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = client.Id }, client);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, Client client)
    {
        if (id != client.Id) return BadRequest();
        _db.Entry(client).State = EntityState.Modified;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return NotFound();
        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
