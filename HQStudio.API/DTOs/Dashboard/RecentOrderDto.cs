using HQStudio.API.Models;

namespace HQStudio.API.DTOs.Dashboard;

public record RecentOrderDto(
    int Id,
    string ClientName,
    OrderStatus Status,
    decimal TotalPrice,
    DateTime CreatedAt
);
