using HQStudio.API.DTOs.Common;
using HQStudio.API.DTOs.Clients;

namespace HQStudio.API.Services.Interfaces;

public interface IClientService
{
    Task<List<ClientDto>> GetAllAsync(int? limit = null, bool isDesktopClient = false);
    Task<ClientDto?> GetByIdAsync(int id);
    Task<Result<ClientDto>> CreateAsync(CreateClientRequest request);
    Task<bool> UpdateAsync(int id, UpdateClientRequest request);
    Task<bool> DeleteAsync(int id);
}
