namespace HQStudio.API.DTOs.Orders;

public record CleanupResult(
    string Message,
    int Deleted,
    List<int> DeletedIds
);
