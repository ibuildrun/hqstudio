using HQStudio.API.Models;

namespace HQStudio.API.DTOs.Auth;

public record RegisterRequest(string Login, string Password, string Name, UserRole Role);
