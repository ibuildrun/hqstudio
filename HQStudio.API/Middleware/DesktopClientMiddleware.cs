namespace HQStudio.API.Middleware;

/// <summary>
/// Middleware для определения типа клиента (Desktop/Web) по заголовку X-Client-Type.
/// Устанавливает HttpContext.Items["IsDesktopClient"] для использования в атрибутах и контроллерах.
/// </summary>
public class DesktopClientMiddleware
{
    private readonly RequestDelegate _next;

    public DesktopClientMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var clientType = context.Request.Headers["X-Client-Type"].FirstOrDefault();
        var isDesktop = clientType?.Equals("Desktop", StringComparison.OrdinalIgnoreCase) == true;
        
        context.Items["IsDesktopClient"] = isDesktop;
        
        await _next(context);
    }
}

/// <summary>
/// Extension методы для регистрации DesktopClientMiddleware
/// </summary>
public static class DesktopClientMiddlewareExtensions
{
    public static IApplicationBuilder UseDesktopClientDetection(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<DesktopClientMiddleware>();
    }
}
