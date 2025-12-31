using HQStudio.API.Models;

namespace HQStudio.API.DTOs.Orders;

public record OrderFilterRequest(
    OrderStatus? Status = null,
    int Page = 1,
    int PageSize = 20,
    bool IncludeDeleted = false
);
