namespace HQStudio.API.Models;

public enum RequestStatus
{
    New,
    Processing,
    Completed,
    Cancelled
}

public enum RequestSource
{
    Website,      // Заявка с сайта
    Phone,        // Телефонный звонок
    WalkIn,       // Живой приход в гараж
    Email,        // Почта
    Messenger,    // Мессенджер (WhatsApp, Telegram)
    Referral,     // По рекомендации
    Other         // Другое
}

public class CallbackRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? CarModel { get; set; }
    public string? LicensePlate { get; set; }
    public string? Message { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.New;
    public RequestSource Source { get; set; } = RequestSource.Website;
    public string? SourceDetails { get; set; } // Дополнительная информация об источнике
    public int? AssignedUserId { get; set; } // Кто взял в работу
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class Subscription
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
