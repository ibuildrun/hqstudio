using System.Security.Claims;

namespace HQStudio.API.Extensions;

/// <summary>
/// Extension методы для HttpContext
/// </summary>
public static class HttpContextExtensions
{
    /// <summary>
    /// Проверяет, является ли текущий запрос от Desktop клиента.
    /// Значение устанавливается DesktopClientMiddleware.
    /// </summary>
    public static bool IsDesktopClient(this HttpContext context)
    {
        return context.Items["IsDesktopClient"] as bool? ?? false;
    }
    
    /// <summary>
    /// Получает ID текущего аутентифицированного пользователя.
    /// Возвращает 0, если пользователь не аутентифицирован.
    /// </summary>
    public static int GetCurrentUserId(this HttpContext context)
    {
        var claim = context.User.FindFirst(ClaimTypes.NameIdentifier);
        return int.TryParse(claim?.Value, out var id) ? id : 0;
    }
    
    /// <summary>
    /// Проверяет, аутентифицирован ли текущий пользователь.
    /// </summary>
    public static bool IsAuthenticated(this HttpContext context)
    {
        return context.User.Identity?.IsAuthenticated == true;
    }
    
    /// <summary>
    /// Проверяет, имеет ли текущий пользователь указанную роль.
    /// </summary>
    public static bool HasRole(this HttpContext context, string role)
    {
        return context.User.IsInRole(role);
    }
}
