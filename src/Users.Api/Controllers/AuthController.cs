using Users.Api.Helpers;
using Users.Application;
using Users.Domain.Entities;
using Users.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Users.Infrastructure.Data;
using Users.Application.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace Users.Api.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    [AllowAnonymous] // Auth controller es público (login, registro, etc.)
    public class AuthController : ControllerBase
    {
        private readonly UsersDbContext _db;
        private readonly IPasswordService _passwordService;
        private readonly ISessionTokenService _tokenService; // Legacy - para compatibilidad
        private readonly IJwtTokenService _jwtTokenService; // Nuevo servicio JWT
        private readonly Users.Application.Services.User.IUserService _userService;

        public AuthController(
            UsersDbContext db,
            IPasswordService passwordService,
            ISessionTokenService tokenService,
            IJwtTokenService jwtTokenService,
            Users.Application.Services.User.IUserService userService)
        {
            _db = db;
            _passwordService = passwordService;
            _tokenService = tokenService;
            _jwtTokenService = jwtTokenService;
            _userService = userService;
        }

        /// <summary>
        /// Autentica un usuario y crea una sesión. Retorna un token de sesión si las credenciales son válidas.
        /// </summary>
        /// <response code="200">Login exitoso, retorna token</response>
        /// <response code="401">Credenciales inválidas</response>
        /// <response code="403">Usuario inactivo o prohibido</response>
        [HttpPost("login")]
        [ProducesResponseType(typeof(LoginResponseDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var user = await _userService.AuthenticateAsync(dto.Email, dto.Password);
            if (user is null)
            {
                return Unauthorized(new { error = Localization.Get("Error_InvalidCredentials", lang) });
            }
            if (user.Status == UserStatus.inactive || user.Status == UserStatus.blocked)
            {
                return Forbid(Localization.Get("Error_UserInactive", lang));
            }

            // Actualizar LastLogin del usuario
            user.LastLogin = DateTime.UtcNow;

            // Generar JWT token real (nuevo)
            var jwtToken = _jwtTokenService.GenerateToken(
                user.Id,
                user.Email,
                user.Role.ToString(),
                $"{user.Name} {user.Lastname}"
            );
            var tokenExpiry = _jwtTokenService.GetTokenExpiration();

            // Guardar hash del token en sesión (para invalidación)
            var tokenHash = _tokenService.HashToken(jwtToken);
            var session = new Session
            {
                UserId = user.Id,
                TokenHash = tokenHash,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = tokenExpiry
            };
            _db.Sessions.Add(session);

            // Guardar cambios: user.LastLogin y session en una transacción
            // Manejar concurrencia para InMemoryDatabase en tests
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                // Recargar el usuario y reintentar
                await _db.Entry(user).ReloadAsync();
                user.LastLogin = DateTime.UtcNow;
                await _db.SaveChangesAsync();
            }

            // Map Preference
            PreferenceReadDto? prefDto = null;
            if (user.Preference != null)
            {
                prefDto = new PreferenceReadDto(
                    user.Preference.Id,
                    user.Preference.UserId,
                    user.Preference.WcagVersion,
                    user.Preference.WcagLevel.ToString(),
                    user.Preference.Language.ToString(),
                    user.Preference.VisualTheme.ToString(),
                    user.Preference.ReportFormat.ToString(),
                    user.Preference.NotificationsEnabled,
                    user.Preference.AiResponseLevel?.ToString(),
                    user.Preference.FontSize,
                    user.Preference.CreatedAt,
                    user.Preference.UpdatedAt
                );
            }
            var userDto = new UserWithPreferenceReadDto(
                user.Id,
                user.Nickname,
                user.Name,
                user.Lastname,
                user.Email,
                user.Role.ToString(),
                user.Status.ToString(),
                user.EmailConfirmed,
                user.LastLogin,
                user.RegistrationDate,
                user.CreatedAt,
                user.UpdatedAt,
                prefDto
            );

            return Ok(new LoginResponseDto(jwtToken, session.ExpiresAt, userDto));
        }

        /// <summary>
        /// Cierra sesión (logout global). Elimina todas las sesiones del usuario y pone last_login en null.
        /// </summary>
        /// <response code="200">Logout exitoso</response>
        /// <response code="404">Usuario no encontrado</response>
        [HttpPost("logout")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Logout([FromBody] LogoutDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (u is null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }
            var sessions = _db.Sessions.Where(s => s.UserId == u.Id);
            _db.Sessions.RemoveRange(sessions);
            u.LastLogin = null;
            await _db.SaveChangesAsync();
            return Ok(new { message = Localization.Get("Success_Logout", lang) });
        }

        /// <summary>
        /// Resetea la contraseña del usuario.
        /// </summary>
        /// <response code="200">Contraseña reseteada</response>
        /// <response code="404">Usuario no encontrado</response>
        [HttpPost("reset-password")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var u = await _db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
            if (u is null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }
            u.Password = _passwordService.Hash(dto.NewPassword);
            u.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = Localization.Get("Success_PasswordReset", lang) });
        }

        /// <summary>
        /// Confirma el email del usuario.
        /// </summary>
        /// <response code="200">Email confirmado</response>
        /// <response code="404">Usuario no encontrado</response>
        [HttpPost("confirm-email/{userId:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ConfirmEmail(int userId)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var u = await _db.Users.FindAsync(userId);
            if (u is null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }
            u.EmailConfirmed = true;
            u.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
            return Ok(new { message = Localization.Get("Success_EmailConfirmed", lang) });
        }
    }
}