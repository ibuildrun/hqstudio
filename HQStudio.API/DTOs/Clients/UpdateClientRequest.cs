namespace HQStudio.API.DTOs.Clients;

public record UpdateClientRequest(
    string Name,
    string Phone,
    string? Email,
    string? CarModel,
    string? LicensePlate,
    string? Notes
);
