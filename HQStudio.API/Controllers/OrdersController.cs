using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _db;

    public OrdersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<Order>>> GetAll([FromQuery] OrderStatus? status = null, [FromQuery] int? limit = null)
    {
        var query = _db.Orders
            .Include(o => o.Client)
            .Include(o => o.OrderServices)
            .ThenInclude(os => os.Service)
            .AsQueryable();

        if (status.HasValue) query = query.Where(o => o.Status == status);

        query = query.OrderByDescending(o => o.CreatedAt);
        
        // Ограничение для веб-клиентов (защита от выкачивания базы)
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
    public async Task<ActionResult<Order>> Get(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Client)
            .Include(o => o.OrderServices)
            .ThenInclude(os => os.Service)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? NotFound() : Ok(order);
    }

    [HttpPost]
    public async Task<ActionResult<Order>> Create(CreateOrderRequest request)
    {
        var order = new Order
        {
            ClientId = request.ClientId,
            Notes = request.Notes,
            TotalPrice = request.TotalPrice
        };

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        foreach (var serviceId in request.ServiceIds)
        {
            var service = await _db.Services.FindAsync(serviceId);
            if (service != null)
            {
                _db.OrderServices.Add(new OrderService
                {
                    OrderId = order.Id,
                    ServiceId = serviceId,
                    Price = 0 // Can be set from service price
                });
            }
        }
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();

        order.Status = status;
        if (status == OrderStatus.Completed) order.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return NotFound();
        _db.Orders.Remove(order);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record CreateOrderRequest(int ClientId, List<int> ServiceIds, decimal TotalPrice, string? Notes);
