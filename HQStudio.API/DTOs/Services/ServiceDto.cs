namespace HQStudio.API.DTOs.Services;

public record ServiceDto(
    int Id,
    string Title,
    string Category,
    string Description,
    string Price,
    string? Image,
    string Icon,
    bool IsActive,
    int SortOrder
);
