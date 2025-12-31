using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace HQStudio.API.Attributes;

/// <summary>
/// Атрибут авторизации, который позволяет Desktop клиентам обходить JWT авторизацию.
/// Для Web клиентов требуется валидный JWT токен и опционально проверка ролей.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class DesktopOrAuthorizeAttribute : Attribute, IAuthorizationFilter
{
    /// <summary>
    /// Роли, разрешённые для Web клиентов (через запятую).
    /// Desktop клиенты игнорируют эту проверку.
    /// </summary>
    public string? Roles { get; set; }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isDesktop = context.HttpContext.Items["IsDesktopClient"] as bool? ?? false;
        
        // Desktop клиенты обходят авторизацию
        if (isDesktop) return;
        
        var user = context.HttpContext.User;
        
        // Проверка аутентификации для Web клиентов
        if (user.Identity?.IsAuthenticated != true)
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Требуется авторизация" });
            return;
        }
        
        // Проверка ролей, если указаны
        if (!string.IsNullOrEmpty(Roles))
        {
            var roles = Roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim());
            
            if (!roles.Any(role => user.IsInRole(role)))
            {
                context.Result = new ForbidResult();
            }
        }
    }
}

/// <summary>
/// Атрибут для эндпоинтов, доступных только Desktop клиентам.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class DesktopOnlyAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var isDesktop = context.HttpContext.Items["IsDesktopClient"] as bool? ?? false;
        
        if (!isDesktop)
        {
            context.Result = new ForbidResult();
        }
    }
}
