namespace HQStudio.API.DTOs.Auth;

public record LoginResponse(string Token, UserDto User, bool MustChangePassword = false);
