using HQStudio.API.DTOs.Common;
using HQStudio.API.DTOs.Orders;
using HQStudio.API.Models;

namespace HQStudio.API.Services.Interfaces;

public interface IOrderService
{
    Task<PagedResult<OrderDto>> GetAllAsync(OrderFilterRequest filter);
    Task<OrderDto?> GetByIdAsync(int id);
    Task<OrderDto> CreateAsync(CreateOrderRequest request);
    Task<bool> UpdateStatusAsync(int id, OrderStatus status);
    Task<bool> SoftDeleteAsync(int id, int? deletedByUserId);
    Task<bool> RestoreAsync(int id);
    Task<bool> PermanentDeleteAsync(int id);
    Task<CleanupResult> CleanupOrdersWithoutClientsAsync();
}
