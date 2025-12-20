using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.Models;
using System.Security.Cryptography;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/updates")]
public class UpdatesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IWebHostEnvironment _env;
    private readonly string _updatesPath;

    public UpdatesController(AppDbContext db, IWebHostEnvironment env)
    {
        _db = db;
        _env = env;
        _updatesPath = Path.Combine(env.ContentRootPath, "Updates");
        
        if (!Directory.Exists(_updatesPath))
            Directory.CreateDirectory(_updatesPath);
    }

    /// <summary>
    /// Проверка наличия обновлений (только для Desktop приложения с заголовком X-Client-Type)
    /// </summary>
    [HttpGet("check")]
    public async Task<ActionResult<UpdateCheckResponse>> CheckForUpdates([FromQuery] string currentVersion)
    {
        // Проверяем что запрос от Desktop приложения
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        if (!clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Unauthorized(new { message = "Доступ запрещён" });
        }

        var latestUpdate = await _db.AppUpdates
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.ReleasedAt)
            .FirstOrDefaultAsync();

        if (latestUpdate == null)
        {
            return Ok(new UpdateCheckResponse
            {
                UpdateAvailable = false,
                CurrentVersion = currentVersion
            });
        }

        var isNewer = CompareVersions(latestUpdate.Version, currentVersion) > 0;

        return Ok(new UpdateCheckResponse
        {
            UpdateAvailable = isNewer,
            CurrentVersion = currentVersion,
            LatestVersion = latestUpdate.Version,
            ReleaseNotes = isNewer ? latestUpdate.ReleaseNotes : null,
            DownloadUrl = isNewer ? $"/api/updates/download/{latestUpdate.Id}" : null,
            FileSize = isNewer ? latestUpdate.FileSize : null,
            Checksum = isNewer ? latestUpdate.Checksum : null,
            IsMandatory = isNewer && latestUpdate.IsMandatory,
            ReleasedAt = isNewer ? latestUpdate.ReleasedAt : null
        });
    }

    /// <summary>
    /// Скачивание файла обновления (только для Desktop приложения)
    /// </summary>
    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadUpdate(int id)
    {
        // Проверяем что запрос от Desktop приложения
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        if (!clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Unauthorized(new { message = "Доступ запрещён" });
        }

        var update = await _db.AppUpdates.FindAsync(id);
        if (update == null || !update.IsActive)
            return NotFound(new { message = "Обновление не найдено" });

        var filePath = Path.Combine(_updatesPath, update.FileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { message = "Файл обновления не найден на сервере" });

        var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(fileBytes, "application/octet-stream", update.FileName);
    }

    /// <summary>
    /// Получить информацию о последней версии (только для Desktop)
    /// </summary>
    [HttpGet("latest")]
    public async Task<ActionResult<AppUpdate>> GetLatestUpdate()
    {
        // Проверяем что запрос от Desktop приложения
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        if (!clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true)
        {
            return Unauthorized(new { message = "Доступ запрещён" });
        }

        var latest = await _db.AppUpdates
            .Where(u => u.IsActive)
            .OrderByDescending(u => u.ReleasedAt)
            .FirstOrDefaultAsync();

        if (latest == null)
            return NotFound(new { message = "Нет доступных обновлений" });

        return Ok(latest);
    }

    /// <summary>
    /// Список всех обновлений (для админки)
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<List<AppUpdate>>> GetAllUpdates()
    {
        return await _db.AppUpdates
            .OrderByDescending(u => u.ReleasedAt)
            .ToListAsync();
    }

    /// <summary>
    /// Загрузить новое обновление (для админки)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(100_000_000)] // 100MB лимит
    public async Task<ActionResult<AppUpdate>> UploadUpdate(
        [FromForm] string version,
        [FromForm] string releaseNotes,
        [FromForm] bool isMandatory,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "Файл не загружен" });

        // Проверяем что версия уникальна
        var existingVersion = await _db.AppUpdates.AnyAsync(u => u.Version == version);
        if (existingVersion)
            return BadRequest(new { message = $"Версия {version} уже существует" });

        // Генерируем имя файла
        var fileName = $"HQStudio_{version.Replace(".", "_")}{Path.GetExtension(file.FileName)}";
        var filePath = Path.Combine(_updatesPath, fileName);

        // Сохраняем файл
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Вычисляем контрольную сумму
        var checksum = await ComputeChecksumAsync(filePath);

        var update = new AppUpdate
        {
            Version = version,
            ReleaseNotes = releaseNotes,
            FileName = fileName,
            FileSize = file.Length,
            Checksum = checksum,
            IsMandatory = isMandatory,
            DownloadUrl = $"/api/updates/download/",
            IsActive = true,
            ReleasedAt = DateTime.UtcNow
        };

        _db.AppUpdates.Add(update);
        await _db.SaveChangesAsync();

        update.DownloadUrl = $"/api/updates/download/{update.Id}";
        await _db.SaveChangesAsync();

        return Ok(update);
    }

    /// <summary>
    /// Деактивировать обновление
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUpdate(int id)
    {
        var update = await _db.AppUpdates.FindAsync(id);
        if (update == null)
            return NotFound();

        update.IsActive = false;
        await _db.SaveChangesAsync();

        return NoContent();
    }

    /// <summary>
    /// Сравнение версий (1.0.0 формат)
    /// </summary>
    private static int CompareVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();
        var parts2 = v2.Split('.').Select(p => int.TryParse(p, out var n) ? n : 0).ToArray();

        var maxLength = Math.Max(parts1.Length, parts2.Length);
        
        for (int i = 0; i < maxLength; i++)
        {
            var p1 = i < parts1.Length ? parts1[i] : 0;
            var p2 = i < parts2.Length ? parts2[i] : 0;
            
            if (p1 > p2) return 1;
            if (p1 < p2) return -1;
        }
        
        return 0;
    }

    /// <summary>
    /// Вычисление SHA256 контрольной суммы
    /// </summary>
    private static async Task<string> ComputeChecksumAsync(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = System.IO.File.OpenRead(filePath);
        var hash = await sha256.ComputeHashAsync(stream);
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
