namespace Users.Application.Services.UserContext;

/// <summary>
/// Contexto del usuario autenticado extraído de los headers HTTP propagados por el Gateway.
/// El Gateway valida el JWT y propaga los claims como headers X-User-*.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// ID del usuario autenticado (extraído de X-User-Id header).
    /// </summary>
    int UserId { get; }

    /// <summary>
    /// Email del usuario autenticado (extraído de X-User-Email header).
    /// </summary>
    string Email { get; }

    /// <summary>
    /// Rol del usuario autenticado (extraído de X-User-Role header).
    /// Valores comunes: "user", "admin"
    /// </summary>
    string Role { get; }

    /// <summary>
    /// Nombre del usuario autenticado (extraído de X-User-Name header).
    /// </summary>
    string UserName { get; }

    /// <summary>
    /// Indica si el usuario está autenticado (tiene X-User-Id header válido).
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Indica si el usuario tiene rol de administrador.
    /// </summary>
    bool IsAdmin { get; }
}