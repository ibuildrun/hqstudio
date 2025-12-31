using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.DTOs.Services;
using HQStudio.API.Models;
using HQStudio.API.Services.Interfaces;

namespace HQStudio.API.Services;

public class ServiceService : IServiceService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ServiceService> _logger;

    public ServiceService(AppDbContext db, ILogger<ServiceService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ServiceDto>> GetAllAsync(bool activeOnly = false)
    {
        var query = _db.Services.AsQueryable();
        
        if (activeOnly) 
            query = query.Where(s => s.IsActive);
        
        return await query
            .OrderBy(s => s.SortOrder)
            .Select(s => MapToDto(s))
            .ToListAsync();
    }

    public async Task<ServiceDto?> GetByIdAsync(int id)
    {
        var service = await _db.Services.FindAsync(id);
        return service == null ? null : MapToDto(service);
    }

    public async Task<ServiceDto> CreateAsync(Service service)
    {
        _db.Services.Add(service);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Created service {ServiceId}: {ServiceTitle}", service.Id, service.Title);
        
        return MapToDto(service);
    }

    public async Task<bool> UpdateAsync(int id, Service service)
    {
        if (id != service.Id) return false;
        
        var existing = await _db.Services.FindAsync(id);
        if (existing == null) return false;
        
        existing.Title = service.Title;
        existing.Category = service.Category;
        existing.Description = service.Description;
        existing.Price = service.Price;
        existing.Image = service.Image;
        existing.Icon = service.Icon;
        existing.IsActive = service.IsActive;
        existing.SortOrder = service.SortOrder;
        
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Updated service {ServiceId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var service = await _db.Services.FindAsync(id);
        if (service == null) return false;
        
        _db.Services.Remove(service);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Deleted service {ServiceId}", id);
        return true;
    }

    private static ServiceDto MapToDto(Service s) => new(
        s.Id,
        s.Title,
        s.Category,
        s.Description,
        s.Price,
        s.Image,
        s.Icon,
        s.IsActive,
        s.SortOrder
    );
}
