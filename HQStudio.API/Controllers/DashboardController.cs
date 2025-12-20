using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;

    public DashboardController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<DashboardStats>> GetStats()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        var completedOrders = await _db.Orders
            .Where(o => o.CompletedAt >= monthStart && o.Status == OrderStatus.Completed)
            .ToListAsync();

        var stats = new DashboardStats
        {
            TotalClients = await _db.Clients.CountAsync(),
            TotalOrders = await _db.Orders.CountAsync(),
            NewCallbacks = await _db.CallbackRequests.CountAsync(c => c.Status == RequestStatus.New),
            MonthlyRevenue = completedOrders.Sum(o => o.TotalPrice),
            OrdersInProgress = await _db.Orders.CountAsync(o => o.Status == OrderStatus.InProgress),
            CompletedThisMonth = completedOrders.Count,
            NewSubscribers = await _db.Subscriptions.CountAsync(s => s.CreatedAt >= monthStart),
            PopularServices = await _db.OrderServices
                .GroupBy(os => os.Service.Title)
                .Select(g => new ServiceStat { Name = g.Key, Count = g.Count() })
                .OrderByDescending(s => s.Count)
                .Take(5)
                .ToListAsync(),
            RecentOrders = await _db.Orders
                .Include(o => o.Client)
                .OrderByDescending(o => o.CreatedAt)
                .Take(5)
                .Select(o => new RecentOrder
                {
                    Id = o.Id,
                    ClientName = o.Client.Name,
                    Status = o.Status,
                    TotalPrice = o.TotalPrice,
                    CreatedAt = o.CreatedAt
                })
                .ToListAsync()
        };

        return Ok(stats);
    }
}

public class DashboardStats
{
    public int TotalClients { get; set; }
    public int TotalOrders { get; set; }
    public int NewCallbacks { get; set; }
    public decimal MonthlyRevenue { get; set; }
    public int OrdersInProgress { get; set; }
    public int CompletedThisMonth { get; set; }
    public int NewSubscribers { get; set; }
    public List<ServiceStat> PopularServices { get; set; } = new();
    public List<RecentOrder> RecentOrders { get; set; } = new();
}

public class ServiceStat
{
    public string Name { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class RecentOrder
{
    public int Id { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public OrderStatus Status { get; set; }
    public decimal TotalPrice { get; set; }
    public DateTime CreatedAt { get; set; }
}
