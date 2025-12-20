using HQStudio.API.Models;

namespace HQStudio.API.DTOs;

public record LoginRequest(string Login, string Password);

public record LoginResponse(string Token, UserDto User, bool MustChangePassword = false);

public record UserDto(int Id, string Login, string Name, UserRole Role);

public record RegisterRequest(string Login, string Password, string Name, UserRole Role);

public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
