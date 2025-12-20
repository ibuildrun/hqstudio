using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HQStudio.API.Data;
using HQStudio.API.DTOs;
using HQStudio.API.Models;

namespace HQStudio.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _db;

    public UsersController(AppDbContext db) => _db = db;

    [HttpGet]
    public async Task<ActionResult<List<UserDto>>> GetAll()
    {
        var users = await _db.Users.Where(u => u.IsActive).ToListAsync();
        return users.Select(u => new UserDto(u.Id, u.Login, u.Name, u.Role)).ToList();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> Get(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        return new UserDto(user.Id, user.Login, user.Name, user.Role);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();

        user.Name = request.Name;
        user.Role = request.Role;
        
        if (!string.IsNullOrEmpty(request.Password))
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        await _db.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        if (user.Login == "admin") return BadRequest(new { message = "Нельзя удалить администратора" });
        
        user.IsActive = false;
        await _db.SaveChangesAsync();
        return NoContent();
    }
}

public record UpdateUserRequest(string Name, UserRole Role, string? Password);
