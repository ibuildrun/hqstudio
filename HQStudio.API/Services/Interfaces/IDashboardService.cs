using HQStudio.API.DTOs.Dashboard;

namespace HQStudio.API.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsDto> GetStatsAsync();
}
