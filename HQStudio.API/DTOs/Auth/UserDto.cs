using HQStudio.API.Models;

namespace HQStudio.API.DTOs.Auth;

public record UserDto(int Id, string Login, string Name, UserRole Role);
