using HQStudio.API.Models;

namespace HQStudio.API.DTOs.Orders;

public record OrderDto(
    int Id,
    int ClientId,
    string ClientName,
    OrderStatus Status,
    decimal TotalPrice,
    string? Notes,
    DateTime CreatedAt,
    DateTime? CompletedAt,
    List<OrderServiceDto> Services
);

public record OrderServiceDto(int ServiceId, string ServiceTitle, decimal Price);
