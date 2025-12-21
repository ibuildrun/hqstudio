namespace HQStudio.API.Models;

/// <summary>
/// Запись журнала ответственности
/// </summary>
public class ActivityLog
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = "";
    public string Action { get; set; } = "";
    public string? EntityType { get; set; }
    public int? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public string Source { get; set; } = "Desktop"; // Desktop, Web, API
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User? User { get; set; }
}
