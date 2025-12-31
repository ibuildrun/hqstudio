using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.DTOs.Common;
using HQStudio.API.DTOs.Clients;
using HQStudio.API.Models;
using HQStudio.API.Services.Interfaces;

namespace HQStudio.API.Services;

public class ClientService : IClientService
{
    private readonly AppDbContext _db;
    private readonly ILogger<ClientService> _logger;

    public ClientService(AppDbContext db, ILogger<ClientService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<ClientDto>> GetAllAsync(int? limit = null, bool isDesktopClient = false)
    {
        var query = _db.Clients
            .OrderByDescending(c => c.CreatedAt)
            .AsQueryable();
        
        // Для Web клиентов ограничиваем количество
        if (!isDesktopClient)
        {
            query = query.Take(20);
        }
        else if (limit.HasValue && limit > 0)
        {
            query = query.Take(limit.Value);
        }
        
        return await query
            .Select(c => new ClientDto(
                c.Id,
                c.Name,
                c.Phone,
                c.Email,
                c.CarModel,
                c.LicensePlate,
                c.Notes,
                c.CreatedAt,
                c.Orders.Count
            ))
            .ToListAsync();
    }

    public async Task<ClientDto?> GetByIdAsync(int id)
    {
        var client = await _db.Clients
            .Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id);
            
        if (client == null) return null;
        
        return new ClientDto(
            client.Id,
            client.Name,
            client.Phone,
            client.Email,
            client.CarModel,
            client.LicensePlate,
            client.Notes,
            client.CreatedAt,
            client.Orders.Count
        );
    }

    public async Task<Result<ClientDto>> CreateAsync(CreateClientRequest request)
    {
        // Форматируем телефон
        var formattedPhone = PhoneFormatter.Format(request.Phone);
        
        // Проверка на дубликат по телефону - загружаем все телефоны и сравниваем на клиенте
        var normalizedPhone = NormalizePhone(formattedPhone);
        var clients = await _db.Clients.Select(c => new { c.Id, c.Name, c.Phone }).ToListAsync();
        var existingByPhone = clients.FirstOrDefault(c => NormalizePhone(c.Phone) == normalizedPhone);
        
        if (existingByPhone != null)
        {
            return Result<ClientDto>.Fail(
                $"Клиент с таким номером телефона уже существует (ID: {existingByPhone.Id}, Имя: {existingByPhone.Name})"
            );
        }
        
        var client = new Client
        {
            Name = request.Name,
            Phone = formattedPhone,
            Email = request.Email,
            CarModel = request.CarModel,
            LicensePlate = request.LicensePlate,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Clients.Add(client);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Created client {ClientId}: {ClientName}", client.Id, client.Name);
        
        return Result<ClientDto>.Ok(new ClientDto(
            client.Id,
            client.Name,
            client.Phone,
            client.Email,
            client.CarModel,
            client.LicensePlate,
            client.Notes,
            client.CreatedAt,
            0
        ));
    }

    public async Task<bool> UpdateAsync(int id, UpdateClientRequest request)
    {
        var existing = await _db.Clients.FindAsync(id);
        if (existing == null) return false;
        
        existing.Name = request.Name;
        existing.Phone = PhoneFormatter.Format(request.Phone);
        existing.Email = request.Email;
        existing.CarModel = request.CarModel;
        existing.LicensePlate = request.LicensePlate;
        existing.Notes = request.Notes;
        
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Updated client {ClientId}", id);
        return true;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var client = await _db.Clients.FindAsync(id);
        if (client == null) return false;
        
        _db.Clients.Remove(client);
        await _db.SaveChangesAsync();
        
        _logger.LogInformation("Deleted client {ClientId}", id);
        return true;
    }

    private static string NormalizePhone(string phone)
    {
        return phone
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("+", "");
    }
}
