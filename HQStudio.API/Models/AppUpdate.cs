namespace HQStudio.API.Models;

public class AppUpdate
{
    public int Id { get; set; }
    public string Version { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public string DownloadUrl { get; set; } = "";
    public string FileName { get; set; } = "";
    public long FileSize { get; set; }
    public string Checksum { get; set; } = ""; // SHA256 для проверки целостности
    public bool IsMandatory { get; set; } // Обязательное обновление
    public bool IsActive { get; set; } = true;
    public DateTime ReleasedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class UpdateCheckResponse
{
    public bool UpdateAvailable { get; set; }
    public string? LatestVersion { get; set; }
    public string? CurrentVersion { get; set; }
    public string? ReleaseNotes { get; set; }
    public string? DownloadUrl { get; set; }
    public long? FileSize { get; set; }
    public string? Checksum { get; set; }
    public bool IsMandatory { get; set; }
    public DateTime? ReleasedAt { get; set; }
}

public class CreateUpdateRequest
{
    public string Version { get; set; } = "";
    public string ReleaseNotes { get; set; } = "";
    public bool IsMandatory { get; set; }
}
