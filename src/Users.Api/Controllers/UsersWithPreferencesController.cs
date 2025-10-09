using Users.Api;
using Users.Api.Helpers;
using Users.Application;
using Users.Domain.Entities;
using Users.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Users.Infrastructure.Data;
using Users.Application.Services;
using Microsoft.EntityFrameworkCore;
using Users.Application.Services.UserContext;

namespace Users.Api.Controllers
{
    [ApiController]
    [Route("api/users-with-preferences")]
    public class UsersWithPreferencesController : ControllerBase
    {
        private static void UpdateUserFields(User user, PreferenceUserPatchDto patchDto, IPasswordService passwordService)
        {
            if (patchDto.Nickname is not null)
            {
                user.Nickname = patchDto.Nickname;
            }

            if (patchDto.Name is not null)
            {
                user.Name = patchDto.Name;
            }

            if (patchDto.Lastname is not null)
            {
                user.Lastname = patchDto.Lastname;
            }

            if (patchDto.Email is not null)
            {
                user.Email = patchDto.Email;
            }

            if (patchDto.Password is not null)
            {
                user.Password = passwordService.Hash(patchDto.Password);
            }

            user.UpdatedAt = DateTime.UtcNow;
        }

        private static void UpdatePreferenceFields(Preference pref, PreferenceUserPatchDto patchDto)
        {
            if (patchDto.WcagVersion is not null && new[] { "2.0", "2.1", "2.2" }.Contains(patchDto.WcagVersion))
            {
                pref.WcagVersion = patchDto.WcagVersion;
            }

            if (patchDto.WcagLevel is not null && Enum.TryParse<WcagLevel>(patchDto.WcagLevel, true, out var wcagLevel))
            {
                pref.WcagLevel = wcagLevel;
            }

            if (patchDto.Language is not null && Enum.TryParse<Language>(patchDto.Language, true, out var language))
            {
                pref.Language = language;
            }

            if (patchDto.VisualTheme is not null && Enum.TryParse<VisualTheme>(patchDto.VisualTheme, true, out var visualTheme))
            {
                pref.VisualTheme = visualTheme;
            }

            if (patchDto.ReportFormat is not null && Enum.TryParse<ReportFormat>(patchDto.ReportFormat, true, out var reportFormat))
            {
                pref.ReportFormat = reportFormat;
            }

            if (patchDto.NotificationsEnabled is not null)
            {
                pref.NotificationsEnabled = patchDto.NotificationsEnabled.Value;
            }

            if (patchDto.AiResponseLevel is not null && Enum.TryParse<AiResponseLevel>(patchDto.AiResponseLevel, true, out var aiLevel))
            {
                pref.AiResponseLevel = aiLevel;
            }

            if (patchDto.FontSize is not null)
            {
                pref.FontSize = patchDto.FontSize.Value;
            }

            pref.UpdatedAt = DateTime.UtcNow;
        }

        private readonly UsersDbContext _db;
        private readonly IPasswordService _passwordService;
        private readonly IUserContext _userContext;

        public UsersWithPreferencesController(UsersDbContext db, IPasswordService passwordService, IUserContext userContext)
        {
            _db = db;
            _passwordService = passwordService;
            _userContext = userContext;
        }

        /// <summary>
        /// Crea un usuario y sus preferencias por defecto en una sola llamada.
        /// </summary>
        /// <remarks>
        /// Este endpoint crea un usuario y sus preferencias iniciales (WCAG 2.1, AA, español, tema claro, PDF, notificaciones activadas, respuesta AI intermedia, tamaño de fuente 14).
        /// </remarks>
        /// <param name="userDto">Datos del usuario a crear</param>
        /// <response code="201">Usuario y preferencias creados exitosamente</response>
        /// <response code="409">Email o nickname ya existen</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="401">No autenticado</response>
        [HttpPost("")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(409)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [Produces("application/json")]
        [Consumes("application/json")]
        [Tags("UsersWithPreferences")]
        public async Task<IActionResult> Create([FromBody] UserCreateDto userDto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Endpoint público - no requiere autenticación (registro de nuevos usuarios)
            if (await _db.Users.AnyAsync(u => u.Email == userDto.Email))
            {
                return Conflict(new { error = Localization.Get("Error_EmailExists", lang) });
            }

            if (await _db.Users.AnyAsync(u => u.Nickname == userDto.Nickname))
            {
                return Conflict(new { error = Localization.Get("Error_NicknameExists", lang) });
            }

            var now = DateTime.UtcNow;
            var user = new User
            {
                Nickname = userDto.Nickname,
                Name = userDto.Name,
                Lastname = userDto.Lastname,
                Email = userDto.Email,
                Password = _passwordService.Hash(userDto.Password),
                Role = UserRole.user,
                Status = UserStatus.active,
                EmailConfirmed = false,
                RegistrationDate = now,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();
            // Preferencias por defecto
            var pref = new Preference
            {
                UserId = user.Id,
                WcagVersion = "2.2",
                WcagLevel = WcagLevel.AA,
                Language = Language.es,
                VisualTheme = VisualTheme.light,
                ReportFormat = ReportFormat.pdf,
                NotificationsEnabled = true,
                AiResponseLevel = AiResponseLevel.intermediate,
                FontSize = 14,
                CreatedAt = now,
                UpdatedAt = now
            };
            _db.Preferences.Add(pref);
            await _db.SaveChangesAsync();

            var result = new
            {
                user = new
                {
                    user.Id,
                    user.Nickname,
                    user.Name,
                    user.Lastname,
                    user.Email
                },
                preferences = new
                {
                    pref.Id,
                    wcagVersion = pref.WcagVersion,
                    wcagLevel = pref.WcagLevel.ToString(),
                    language = pref.Language.ToString(),
                    visualTheme = pref.VisualTheme.ToString(),
                    reportFormat = pref.ReportFormat.ToString(),
                    pref.NotificationsEnabled,
                    aiResponseLevel = pref.AiResponseLevel.ToString(),
                    pref.FontSize
                }
            };
            return Created($"/api/users-with-preferences/{user.Id}", new { result.user, result.preferences, message = Localization.Get("Success_UserAndPreferencesCreated", lang) });
        }
        /// <summary>
        /// Actualiza parcialmente los datos del usuario y/o sus preferencias.
        /// </summary>
        /// <param name="email">Email del usuario a actualizar</param>
        /// <param name="patchDto">Datos opcionales a modificar (usuario y/o preferencias)</param>
        /// <response code="200">Usuario y/o preferencias actualizados</response>
        /// <response code="404">Usuario o preferencias no encontrados</response>
        /// <response code="400">Datos inválidos</response>
        /// <response code="401">No autenticado</response>
        [HttpPatch("by-email/{email}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [Consumes("application/json")]
        [Tags("UsersWithPreferences")]
        public async Task<IActionResult> Patch(string email, [FromBody] PreferenceUserPatchDto patchDto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }

            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user == null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }

            var pref = await _db.Preferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (pref == null)
            {
                return NotFound(new { error = Localization.Get("Error_PreferencesNotFound", lang) });
            }

            UpdateUserFields(user, patchDto, _passwordService);
            UpdatePreferenceFields(pref, patchDto);
            await _db.SaveChangesAsync();

            var result = new
            {
                user = new
                {
                    user.Id,
                    user.Nickname,
                    user.Name,
                    user.Lastname,
                    user.Email
                },
                preferences = new
                {
                    pref.Id,
                    wcagVersion = pref.WcagVersion,
                    wcagLevel = pref.WcagLevel.ToString(),
                    language = pref.Language.ToString(),
                    visualTheme = pref.VisualTheme.ToString(),
                    reportFormat = pref.ReportFormat.ToString(),
                    pref.NotificationsEnabled,
                    aiResponseLevel = pref.AiResponseLevel.ToString(),
                    pref.FontSize
                }
            };
            return Ok(new { result.user, result.preferences, message = Localization.Get("Success_UserAndPreferencesUpdated", lang) });
        }
    }
}