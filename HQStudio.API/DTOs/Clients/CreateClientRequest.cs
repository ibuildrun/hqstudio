namespace HQStudio.API.DTOs.Clients;

public record CreateClientRequest(
    string Name,
    string Phone,
    string? Email,
    string? CarModel,
    string? LicensePlate,
    string? Notes
);
