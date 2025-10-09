namespace Users.Application.Services.UserContext;

/// <summary>
/// Implementación del contexto del usuario autenticado.
/// Los valores son poblados por UserContextMiddleware desde los headers X-User-*.
/// </summary>
public class UserContext : IUserContext
{
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;

    /// <summary>
    /// Usuario está autenticado si tiene UserId > 0
    /// </summary>
    public bool IsAuthenticated => UserId > 0;

    /// <summary>
    /// Usuario es administrador si tiene role "admin" (case insensitive)
    /// </summary>
    public bool IsAdmin => !string.IsNullOrEmpty(Role) &&
                          Role.Equals("admin", StringComparison.OrdinalIgnoreCase);
}