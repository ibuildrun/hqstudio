using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.DTOs;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    /// <summary>
    /// Получить всех пользователей (для Desktop - всех, для Web - только активных)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<UserDetailDto>>> GetAll([FromQuery] bool includeInactive = false)
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        // Для веб-клиентов требуется авторизация
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        // Desktop получает всех пользователей, Web - только активных
        var query = _db.Users.AsQueryable();
        if (!isDesktopClient && !includeInactive)
        {
            query = query.Where(u => u.IsActive);
        }
        
        var users = await query.OrderBy(u => u.Name).ToListAsync();
        
        // Получаем активные сессии для определения онлайн-статуса
        var activeSessions = await _db.UserSessions
            .Where(s => s.Status == UserStatus.Online && s.LastHeartbeat > DateTime.UtcNow.AddMinutes(-5))
            .Select(s => s.UserId)
            .Distinct()
            .ToListAsync();
        
        return users.Select(u => new UserDetailDto(
            u.Id, 
            u.Login, 
            u.Name, 
            u.Role.ToString(),
            u.IsActive,
            activeSessions.Contains(u.Id),
            u.CreatedAt,
            u.CanAccessWeb,
            u.CanAccessDesktop,
            u.WebRole?.ToString(),
            u.DesktopRole?.ToString()
        )).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDetailDto>> Get(int id)
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        
        var isOnline = await _db.UserSessions
            .AnyAsync(s => s.UserId == id && s.Status == UserStatus.Online && s.LastHeartbeat > DateTime.UtcNow.AddMinutes(-5));
        
        return new UserDetailDto(
            user.Id, user.Login, user.Name, user.Role.ToString(), 
            user.IsActive, isOnline, user.CreatedAt,
            user.CanAccessWeb, user.CanAccessDesktop,
            user.WebRole?.ToString(), user.DesktopRole?.ToString()
        );
    }

    [HttpPost]
    public async Task<ActionResult<UserDetailDto>> Create(CreateUserRequest request)
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        // Проверяем уникальность логина
        if (await _db.Users.AnyAsync(u => u.Login == request.Login))
        {
            return BadRequest(new { message = "Пользователь с таким логином уже существует" });
        }
        
        var user = new User
        {
            Login = request.Login,
            Name = request.Name,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = Enum.TryParse<UserRole>(request.Role, out var role) ? role : UserRole.Manager,
            IsActive = true,
            CanAccessWeb = request.CanAccessWeb ?? true,
            CanAccessDesktop = request.CanAccessDesktop ?? true,
            WebRole = !string.IsNullOrEmpty(request.WebRole) && Enum.TryParse<UserRole>(request.WebRole, out var webRole) ? webRole : null,
            DesktopRole = !string.IsNullOrEmpty(request.DesktopRole) && Enum.TryParse<UserRole>(request.DesktopRole, out var desktopRole) ? desktopRole : null,
            CreatedAt = DateTime.UtcNow
        };
        
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        
        return Ok(new UserDetailDto(
            user.Id, user.Login, user.Name, user.Role.ToString(), 
            user.IsActive, false, user.CreatedAt,
            user.CanAccessWeb, user.CanAccessDesktop,
            user.WebRole?.ToString(), user.DesktopRole?.ToString()
        ));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Name = request.Name;
        if (Enum.TryParse<UserRole>(request.Role, out var role))
        {
            user.Role = role;
        }
        
        // Обновляем права доступа
        if (request.CanAccessWeb.HasValue)
            user.CanAccessWeb = request.CanAccessWeb.Value;
        if (request.CanAccessDesktop.HasValue)
            user.CanAccessDesktop = request.CanAccessDesktop.Value;
        
        // Обновляем роли для платформ
        if (request.WebRole != null)
            user.WebRole = Enum.TryParse<UserRole>(request.WebRole, out var webRole) ? webRole : null;
        if (request.DesktopRole != null)
            user.DesktopRole = Enum.TryParse<UserRole>(request.DesktopRole, out var desktopRole) ? desktopRole : null;
        
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Деактивировать/активировать пользователя
    /// </summary>
    [HttpPut("{id}/toggle-active")]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        
        if (user.Login == "admin")
        {
            return BadRequest(new { message = "Нельзя деактивировать администратора" });
        }
        
        user.IsActive = !user.IsActive;
        
        // Если деактивируем - завершаем все активные сессии
        if (!user.IsActive)
        {
            var sessions = await _db.UserSessions.Where(s => s.UserId == id && s.Status == UserStatus.Online).ToListAsync();
            foreach (var session in sessions)
            {
                session.Status = UserStatus.Offline;
                session.EndedAt = DateTime.UtcNow;
            }
        }
        
        await _db.SaveChangesAsync();
        return Ok(new { isActive = user.IsActive });
    }

    /// <summary>
    /// Удаление = деактивация (soft delete)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (user.Login == "admin") return BadRequest(new { message = "Нельзя удалить администратора" });
        
        // Soft delete - просто деактивируем
        user.IsActive = false;
        
        // Завершаем все активные сессии
        var sessions = await _db.UserSessions.Where(s => s.UserId == id && s.Status == UserStatus.Online).ToListAsync();
        foreach (var session in sessions)
        {
            session.Status = UserStatus.Offline;
            session.EndedAt = DateTime.UtcNow;
        }
        
        await _db.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>
    /// Сброс пароля пользователя (пароль = логин, требуется смена при входе)
    /// </summary>
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(int id)
    {
        var clientType = Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktopClient = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        if (!isDesktopClient && !User.Identity?.IsAuthenticated == true)
        {
            return Unauthorized(new { message = "Требуется авторизация" });
        }
        
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        
        // Сбрасываем пароль на логин
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(user.Login);
        user.MustChangePassword = true;
        
        await _db.SaveChangesAsync();
        
        return Ok(new { message = "Пароль сброшен", newPassword = user.Login });
    }
}

public record CreateUserRequest(
    string Login, 
    string Name, 
    string Password, 
    string Role,
    bool? CanAccessWeb = true,
    bool? CanAccessDesktop = true,
    string? WebRole = null,
    string? DesktopRole = null
);

public record UpdateUserRequest(
    string Name, 
    string Role, 
    string? Password = null,
    bool? CanAccessWeb = null,
    bool? CanAccessDesktop = null,
    string? WebRole = null,
    string? DesktopRole = null
);

public record UserDetailDto(
    int Id, 
    string Login, 
    string Name, 
    string Role, 
    bool IsActive, 
    bool IsOnline, 
    DateTime CreatedAt,
    bool CanAccessWeb = true,
    bool CanAccessDesktop = true,
    string? WebRole = null,
    string? DesktopRole = null
);
