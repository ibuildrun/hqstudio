using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.DTOs.Common;
using HQStudio.API.DTOs.Orders;
using HQStudio.API.Models;
using HQStudio.API.Services.Interfaces;

namespace HQStudio.API.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<PagedResult<OrderDto>> GetAllAsync(OrderFilterRequest filter)
    {
        var query = _db.Orders
            .Include(o => o.Client)
            .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
            .AsQueryable();

        if (!filter.IncludeDeleted)
            query = query.Where(o => !o.IsDeleted);

        if (filter.Status.HasValue)
            query = query.Where(o => o.Status == filter.Status);

        var total = await query.CountAsync();
        
        var items = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(o => MapToDto(o))
            .ToListAsync();

        return new PagedResult<OrderDto>(items, total, filter.Page, filter.PageSize);
    }

    public async Task<OrderDto?> GetByIdAsync(int id)
    {
        var order = await _db.Orders
            .Include(o => o.Client)
            .Include(o => o.OrderServices)
                .ThenInclude(os => os.Service)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order == null ? null : MapToDto(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderRequest request)
    {
        var order = new Order
        {
            ClientId = request.ClientId,
            Notes = request.Notes,
            TotalPrice = request.TotalPrice,
            CreatedAt = DateTime.UtcNow
        };

        _db.Orders.Add(order);
        
        // Добавляем услуги в той же транзакции
        foreach (var serviceId in request.ServiceIds)
        {
            var service = await _db.Services.FindAsync(serviceId);
            if (service != null)
            {
                _db.OrderServices.Add(new Models.OrderService
                {
                    Order = order,
                    ServiceId = serviceId,
                    Price = 0
                });
            }
        }
        
        await _db.SaveChangesAsync(); // Один SaveChanges на всю операцию
        
        _logger.LogInformation("Created order {OrderId} for client {ClientId}", order.Id, order.ClientId);
        
        return (await GetByIdAsync(order.Id))!;
    }

    public async Task<bool> UpdateStatusAsync(int id, OrderStatus status)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return false;

        order.Status = status;
        if (status == OrderStatus.Completed) 
            order.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Updated order {OrderId} status to {Status}", id, status);
        return true;
    }

    public async Task<bool> SoftDeleteAsync(int id, int? deletedByUserId)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return false;

        order.IsDeleted = true;
        order.DeletedAt = DateTime.UtcNow;
        order.DeletedByUserId = deletedByUserId;

        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Soft deleted order {OrderId} by user {UserId}", id, deletedByUserId);
        return true;
    }

    public async Task<bool> RestoreAsync(int id)
    {
        var order = await _db.Orders.FindAsync(id);
        if (order == null) return false;

        order.IsDeleted = false;
        order.DeletedAt = null;
        order.DeletedByUserId = null;

        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Restored order {OrderId}", id);
        return true;
    }

    public async Task<bool> PermanentDeleteAsync(int id)
    {
        var order = await _db.Orders
            .Include(o => o.OrderServices)
            .FirstOrDefaultAsync(o => o.Id == id);
            
        if (order == null) return false;

        _db.OrderServices.RemoveRange(order.OrderServices);
        _db.Orders.Remove(order);

        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Permanently deleted order {OrderId}", id);
        return true;
    }

    public async Task<CleanupResult> CleanupOrdersWithoutClientsAsync()
    {
        var clientIds = await _db.Clients.Select(c => c.Id).ToListAsync();
        
        var ordersWithoutClients = await _db.Orders
            .Include(o => o.OrderServices)
            .Where(o => o.ClientId == 0 || !clientIds.Contains(o.ClientId))
            .ToListAsync();

        if (!ordersWithoutClients.Any())
        {
            return new CleanupResult("Заказов без клиентов не найдено", 0, new List<int>());
        }

        var deletedIds = ordersWithoutClients.Select(o => o.Id).ToList();
        
        foreach (var order in ordersWithoutClients)
        {
            _db.OrderServices.RemoveRange(order.OrderServices);
        }
        
        _db.Orders.RemoveRange(ordersWithoutClients);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Cleaned up {Count} orders without clients", ordersWithoutClients.Count);
        
        return new CleanupResult(
            $"Удалено {ordersWithoutClients.Count} заказов без клиентов",
            ordersWithoutClients.Count,
            deletedIds
        );
    }

    private static OrderDto MapToDto(Order o) => new(
        o.Id,
        o.ClientId,
        o.Client?.Name ?? "",
        o.Status,
        o.TotalPrice,
        o.Notes,
        o.CreatedAt,
        o.CompletedAt,
        o.OrderServices.Select(os => new OrderServiceDto(
            os.ServiceId,
            os.Service?.Title ?? "",
            os.Price
        )).ToList()
    );
}
