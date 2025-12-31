using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.DTOs.Auth;
using HQStudio.API.Extensions;
using HQStudio.API.Models;
using HQStudio.API.Services;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly JwtService _jwt;

    public AuthController(AppDbContext db, JwtService jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<LoginResponse>> Login(LoginRequest request)
    {
        // Валидация входных данных
        if (string.IsNullOrWhiteSpace(request.Login) || request.Login.Length > 50)
            return BadRequest(new { message = "Некорректный логин" });
        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 100)
            return BadRequest(new { message = "Некорректный пароль" });

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Login.ToLower() == request.Login.ToLower().Trim() && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return Unauthorized(new { message = "Неверный логин или пароль" });

        // Определяем платформу
        var isDesktopClient = HttpContext.IsDesktopClient();
        var platform = isDesktopClient ? "Desktop" : "Web";

        // Проверяем права доступа к платформе
        if (isDesktopClient && !user.CanAccessDesktop)
            return Unauthorized(new { message = "У вас нет доступа к Desktop приложению" });
        if (!isDesktopClient && !user.CanAccessWeb)
            return Unauthorized(new { message = "У вас нет доступа к Web панели" });

        // Получаем роль для конкретной платформы
        var effectiveRole = user.GetRoleForPlatform(platform);
        
        var token = _jwt.GenerateToken(user, effectiveRole);
        var userDto = new UserDto(user.Id, user.Login, user.Name, effectiveRole);

        return Ok(new LoginResponse(token, userDto, user.MustChangePassword));
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("register")]
    public async Task<ActionResult<UserDto>> Register(RegisterRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.Login.ToLower() == request.Login.ToLower()))
            return BadRequest(new { message = "Пользователь с таким логином уже существует" });

        var user = new User
        {
            Login = request.Login,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Name = request.Name,
            Role = request.Role
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok(new UserDto(user.Id, user.Login, user.Name, user.Role));
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userId = HttpContext.GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        
        if (user == null) return NotFound();
        
        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            return BadRequest(new { message = "Неверный текущий пароль" });
        
        if (request.NewPassword.Length < 6)
            return BadRequest(new { message = "Новый пароль должен быть не менее 6 символов" });
        
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        await _db.SaveChangesAsync();
        
        return Ok(new { message = "Пароль успешно изменён" });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        var userId = HttpContext.GetCurrentUserId();
        var user = await _db.Users.FindAsync(userId);
        
        if (user == null) return NotFound();
        
        return Ok(new UserDto(user.Id, user.Login, user.Name, user.Role));
    }
}
