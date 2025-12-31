namespace HQStudio.API.DTOs.Dashboard;

public record DashboardStatsDto(
    int TotalClients,
    int TotalOrders,
    int NewCallbacks,
    decimal MonthlyRevenue,
    int OrdersInProgress,
    int CompletedThisMonth,
    int NewSubscribers,
    List<ServiceStatDto> PopularServices,
    List<RecentOrderDto> RecentOrders
);
