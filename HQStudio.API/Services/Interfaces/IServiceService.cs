using HQStudio.API.DTOs.Services;
using HQStudio.API.Models;

namespace HQStudio.API.Services.Interfaces;

public interface IServiceService
{
    Task<List<ServiceDto>> GetAllAsync(bool activeOnly = false);
    Task<ServiceDto?> GetByIdAsync(int id);
    Task<ServiceDto> CreateAsync(Service service);
    Task<bool> UpdateAsync(int id, Service service);
    Task<bool> DeleteAsync(int id);
}
