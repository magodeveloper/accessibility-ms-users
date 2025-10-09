using Users.Application.Services.UserContext;

namespace Users.Api.Middleware
{
    /// <summary>
    /// Middleware que extrae la información del usuario autenticado desde los headers HTTP.
    /// El Gateway valida el JWT y propaga los claims como headers X-User-*.
    /// Este middleware lee esos headers y popula el IUserContext para usarlo en controllers/services.
    /// </summary>
    public class UserContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<UserContextMiddleware> _logger;

        public UserContextMiddleware(RequestDelegate next, ILogger<UserContextMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IUserContext userContext)
        {
            try
            {
                // Extraer headers X-User-* propagados por el Gateway
                var userIdHeader = context.Request.Headers["X-User-Id"].FirstOrDefault();
                var emailHeader = context.Request.Headers["X-User-Email"].FirstOrDefault();
                var roleHeader = context.Request.Headers["X-User-Role"].FirstOrDefault();
                var userNameHeader = context.Request.Headers["X-User-Name"].FirstOrDefault();

                var userContextImpl = userContext as UserContext;
                if (userContextImpl == null)
                {
                    _logger.LogWarning("UserContext is not of type UserContext");
                    await _next(context);
                    return;
                }

                // Prioridad 1: Poblar desde headers X-User-* (Gateway)
                if (!string.IsNullOrEmpty(userIdHeader) && int.TryParse(userIdHeader, out var userId))
                {
                    userContextImpl.UserId = userId;
                    userContextImpl.Email = emailHeader ?? string.Empty;
                    userContextImpl.Role = roleHeader ?? string.Empty;
                    userContextImpl.UserName = userNameHeader ?? string.Empty;

                    _logger.LogInformation(
                        "User context populated from headers - UserId: {UserId}, Email: {Email}, Role: {Role}",
                        userId, emailHeader, roleHeader);
                }
                // Prioridad 2: Poblar desde JWT claims (acceso directo/Swagger)
                else if (context.User?.Identity?.IsAuthenticated == true)
                {
                    var claims = context.User.Claims;
                    var userIdClaim = claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid" || c.Type == "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
                    var emailClaim = claims.FirstOrDefault(c => c.Type == "email")?.Value;
                    var nameClaim = claims.FirstOrDefault(c => c.Type == "name")?.Value;
                    var roleClaim = claims.FirstOrDefault(c => c.Type == "role" || c.Type == "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")?.Value;

                    if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out var jwtUserId))
                    {
                        userContextImpl.UserId = jwtUserId;
                        userContextImpl.Email = emailClaim ?? string.Empty;
                        userContextImpl.UserName = nameClaim ?? string.Empty;
                        userContextImpl.Role = roleClaim ?? string.Empty;

                        _logger.LogInformation(
                            "User context populated from JWT - UserId: {UserId}, Email: {Email}, Role: {Role}",
                            jwtUserId, emailClaim, roleClaim);
                    }
                }
                else
                {
                    // No hay headers ni JWT - request anónimo
                    _logger.LogDebug("No authentication found - anonymous request");
                }
            }
            catch (Exception ex)
            {
                // No bloquear el request si hay error extrayendo user context
                _logger.LogError(ex, "Error extracting user context");
            }

            // Continuar con el siguiente middleware
            await _next(context);
        }
    }

    /// <summary>
    /// Extension method para registrar el UserContextMiddleware en el pipeline.
    /// </summary>
    public static class UserContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseUserContext(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<UserContextMiddleware>();
        }
    }
}