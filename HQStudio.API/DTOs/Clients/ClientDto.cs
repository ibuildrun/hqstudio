namespace HQStudio.API.DTOs.Clients;

public record ClientDto(
    int Id,
    string Name,
    string Phone,
    string? Email,
    string? CarModel,
    string? LicensePlate,
    string? Notes,
    DateTime CreatedAt,
    int OrdersCount
);
