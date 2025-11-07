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
        [AllowAnonymous] // Endpoint público
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
        /// Registra un nuevo usuario en el sistema. Endpoint público para auto-registro.
        /// </summary>
        /// <response code="201">Usuario creado exitosamente</response>
        /// <response code="400">Datos inválidos o email ya existe</response>
        /// <response code="409">Email ya registrado</response>
        [HttpPost("register")]
        [AllowAnonymous] // Endpoint público
        [ProducesResponseType(typeof(UserWithPreferenceReadDto), 201)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        public async Task<IActionResult> Register([FromBody] RegisterDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar que el email no exista
            var existingUser = await _db.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == dto.Email.ToLower());

            if (existingUser != null)
            {
                return Conflict(new { error = Localization.Get("Error_EmailAlreadyExists", lang) });
            }

            // Validar fortaleza de password (mínimo 6 caracteres)
            if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
            {
                return BadRequest(new { error = Localization.Get("Error_PasswordTooShort", lang) });
            }

            // Crear nuevo usuario con valores por defecto seguros
            var newUser = new Users.Domain.Entities.User
            {
                Nickname = dto.Nickname ?? dto.Email.Split('@')[0],
                Name = dto.Name,
                Lastname = dto.Lastname,
                Email = dto.Email.ToLower(),
                Password = _passwordService.Hash(dto.Password),
                Role = UserRole.user, // Por defecto usuario regular (no admin)
                Status = UserStatus.active,
                EmailConfirmed = false, // Requiere confirmación
                RegistrationDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Users.Add(newUser);

            // Crear preferencias por defecto
            var defaultPreference = new Users.Domain.Entities.Preference
            {
                User = newUser,
                WcagVersion = "2.2",
                WcagLevel = WcagLevel.AA,
                Language = Language.es,
                VisualTheme = VisualTheme.light,
                ReportFormat = ReportFormat.pdf,
                NotificationsEnabled = true,
                AiResponseLevel = AiResponseLevel.detailed,
                FontSize = 16,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _db.Preferences.Add(defaultPreference);

            await _db.SaveChangesAsync();

            // Recargar para obtener IDs generados
            await _db.Entry(newUser).ReloadAsync();
            await _db.Entry(defaultPreference).ReloadAsync();

            // Retornar usuario creado sin password
            var prefDto = new PreferenceReadDto(
                defaultPreference.Id,
                newUser.Id,
                defaultPreference.WcagVersion,
                defaultPreference.WcagLevel.ToString(),
                defaultPreference.Language.ToString(),
                defaultPreference.VisualTheme.ToString(),
                defaultPreference.ReportFormat.ToString(),
                defaultPreference.NotificationsEnabled,
                defaultPreference.AiResponseLevel?.ToString(),
                defaultPreference.FontSize,
                defaultPreference.CreatedAt,
                defaultPreference.UpdatedAt
            );

            var userDto = new UserWithPreferenceReadDto(
                newUser.Id,
                newUser.Nickname,
                newUser.Name,
                newUser.Lastname,
                newUser.Email,
                newUser.Role.ToString(),
                newUser.Status.ToString(),
                newUser.EmailConfirmed,
                newUser.LastLogin,
                newUser.RegistrationDate,
                newUser.CreatedAt,
                newUser.UpdatedAt,
                prefDto
            );

            return CreatedAtAction(nameof(Register), new { id = newUser.Id }, userDto);
        }

        /// <summary>
        /// Cierra sesión (logout global). Elimina todas las sesiones del usuario y pone last_login en null.
        /// </summary>
        /// <response code="200">Logout exitoso</response>
        /// <response code="401">No autenticado</response>
        /// <response code="404">Usuario no encontrado</response>
        [HttpPost("logout")]
        [Authorize] // Requiere autenticación
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(401)]
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
        [AllowAnonymous] // Endpoint público - En producción debería requerir token de email
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
        /// Cambia la contraseña del usuario autenticado. Requiere contraseña actual.
        /// </summary>
        /// <response code="200">Contraseña cambiada exitosamente</response>
        /// <response code="400">Contraseña actual incorrecta o validación fallida</response>
        /// <response code="401">No autenticado</response>
        /// <response code="404">Usuario no encontrado</response>
        [HttpPost("change-password")]
        [Authorize] // Requiere autenticación JWT
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Obtener email del token JWT del usuario autenticado
            var authenticatedEmail = User.FindFirst("email")?.Value
                ?? User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(authenticatedEmail))
            {
                return Unauthorized(new { error = Localization.Get("Error_InvalidToken", lang) });
            }

            var user = await _db.Users.FirstOrDefaultAsync(x => x.Email == authenticatedEmail);
            if (user is null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }

            // Validar contraseña actual
            if (!_passwordService.Verify(dto.CurrentPassword, user.Password))
            {
                return BadRequest(new { error = Localization.Get("Error_CurrentPasswordIncorrect", lang) });
            }

            // Validar que la nueva contraseña sea diferente
            if (_passwordService.Verify(dto.NewPassword, user.Password))
            {
                return BadRequest(new { error = Localization.Get("Error_NewPasswordSameAsOld", lang) });
            }

            // Validar fortaleza de password
            if (string.IsNullOrWhiteSpace(dto.NewPassword) || dto.NewPassword.Length < 6)
            {
                return BadRequest(new { error = Localization.Get("Error_PasswordTooShort", lang) });
            }

            // Actualizar contraseña
            user.Password = _passwordService.Hash(dto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new { message = Localization.Get("Success_PasswordChanged", lang) });
        }

        /// <summary>
        /// Confirma el email del usuario.
        /// </summary>
        /// <response code="200">Email confirmado</response>
        /// <response code="404">Usuario no encontrado</response>
        [HttpPost("confirm-email/{userId:int}")]
        [AllowAnonymous] // Endpoint público - se usa desde link en email
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