namespace HQStudio.API.DTOs.Orders;

public record CreateOrderRequest(
    int ClientId,
    List<int> ServiceIds,
    decimal TotalPrice,
    string? Notes
);
