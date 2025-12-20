namespace HQStudio.API.Models;

public class UserSession
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;
    
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    
    public UserStatus Status { get; set; } = UserStatus.Online;
    public DateTime LastHeartbeat { get; set; } = DateTime.UtcNow;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    
    public string? IpAddress { get; set; }
}

public enum UserStatus
{
    Online,      // Активен, heartbeat < 30 сек
    Away,        // Неактивен, heartbeat 30 сек - 5 мин
    Offline,     // Оффлайн, heartbeat > 5 мин или явный выход
    Disconnected // Потеря соединения (не было явного выхода)
}
