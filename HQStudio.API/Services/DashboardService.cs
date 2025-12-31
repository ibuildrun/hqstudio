using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.DTOs.Dashboard;
using HQStudio.API.Models;
using HQStudio.API.Services.Interfaces;

namespace HQStudio.API.Services;

public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;

    public DashboardService(AppDbContext db) => _db = db;

    public async Task<DashboardStatsDto> GetStatsAsync()
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);

        // Выполняем запросы последовательно, чтобы избежать проблем с concurrent DbContext access
        var totalClients = await _db.Clients.CountAsync();
        var newCallbacks = await _db.CallbackRequests.CountAsync(c => c.Status == RequestStatus.New);
        var newSubscribers = await _db.Subscriptions.CountAsync(s => s.CreatedAt >= monthStart);
        
        // Запрос для заказов с агрегацией
        var ordersStats = await _db.Orders
            .Where(o => !o.IsDeleted)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                InProgress = g.Count(o => o.Status == OrderStatus.InProgress),
                CompletedThisMonth = g.Count(o => 
                    o.Status == OrderStatus.Completed && 
                    o.CompletedAt.HasValue && 
                    o.CompletedAt.Value >= monthStart),
                MonthlyRevenue = g.Where(o => 
                    o.Status == OrderStatus.Completed && 
                    o.CompletedAt.HasValue && 
                    o.CompletedAt.Value >= monthStart)
                    .Sum(o => o.TotalPrice)
            })
            .FirstOrDefaultAsync();

        // Популярные услуги - загружаем данные и группируем на клиенте
        var orderServicesData = await _db.OrderServices
            .Where(os => !os.Order.IsDeleted)
            .Select(os => os.Service.Title)
            .ToListAsync();
        
        var popularServices = orderServicesData
            .GroupBy(title => title)
            .Select(g => new ServiceStatDto(g.Key, g.Count()))
            .OrderByDescending(s => s.Count)
            .Take(5)
            .ToList();

        // Последние заказы - один запрос с проекцией
        var recentOrders = await _db.Orders
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.CreatedAt)
            .Take(5)
            .Select(o => new RecentOrderDto(
                o.Id,
                o.Client.Name,
                o.Status,
                o.TotalPrice,
                o.CreatedAt
            ))
            .ToListAsync();

        return new DashboardStatsDto(
            TotalClients: totalClients,
            TotalOrders: ordersStats?.Total ?? 0,
            NewCallbacks: newCallbacks,
            MonthlyRevenue: ordersStats?.MonthlyRevenue ?? 0,
            OrdersInProgress: ordersStats?.InProgress ?? 0,
            CompletedThisMonth: ordersStats?.CompletedThisMonth ?? 0,
            NewSubscribers: newSubscribers,
            PopularServices: popularServices,
            RecentOrders: recentOrders
        );
    }
}
