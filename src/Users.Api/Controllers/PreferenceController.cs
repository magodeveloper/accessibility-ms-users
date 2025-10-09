using Users.Api;
using Users.Api.Helpers;
using Users.Application;
using Users.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Users.Application.Services.Preference;
using Users.Application.Services.UserContext;

namespace Users.Api.Controllers
{
    [ApiController]
    [Route("api/preferences")]
    [Authorize] // Requiere autenticación JWT
    public class PreferenceController : ControllerBase
    {
        private readonly IPreferenceService _preferenceService;
        private readonly IUserContext _userContext;

        public PreferenceController(IPreferenceService preferenceService, IUserContext userContext)
        {
            _preferenceService = preferenceService;
            _userContext = userContext;
        }

        // GET: api/preferences/by-user/{email}
        [HttpGet("by-user/{email}")]
        /// <summary>
        /// Obtiene las preferencias de un usuario por su email.
        /// </summary>
        /// <param name="email">Email del usuario cuyas preferencias se buscan.</param>
        /// <response code="200">Preferencias encontradas</response>
        /// <response code="404">Preferencias no encontradas</response>
        /// <response code="401">No autenticado</response>
        public async Task<IActionResult> GetByUserEmail(string email)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }
            var prefs = await _preferenceService.GetAllPreferencesAsync();
            var pref = prefs.FirstOrDefault(p => p.User != null && p.User.Email == email);
            if (pref is null)
            {
                return NotFound(new { error = Localization.Get("Error_PreferencesNotFound", lang) });
            }
            var dto = new
            {
                pref.Id,
                pref.UserId,
                wcagVersion = pref.WcagVersion,
                wcagLevel = pref.WcagLevel.ToString(),
                language = pref.Language.ToString(),
                visualTheme = pref.VisualTheme.ToString(),
                reportFormat = pref.ReportFormat.ToString(),
                pref.NotificationsEnabled,
                aiResponseLevel = pref.AiResponseLevel.ToString(),
                pref.FontSize,
                pref.CreatedAt,
                pref.UpdatedAt,
                user = new
                {
                    pref.User.Id,
                    pref.User.Nickname,
                    pref.User.Name,
                    pref.User.Lastname,
                    email = pref.User.Email,
                    role = pref.User.Role.ToString(),
                    status = pref.User.Status.ToString(),
                    emailConfirmed = pref.User.EmailConfirmed
                }
            };
            return Ok(new { preferences = dto, message = Localization.Get("Success_PreferencesFound", lang) });
        }
        // POST: api/preferences
        /// <summary>
        /// Crea preferencias para un usuario.
        /// </summary>
        /// <response code="201">Preferencias creadas exitosamente</response>
        /// <response code="409">Conflicto al crear preferencias</response>
        /// <response code="401">No autenticado</response>
        [HttpPost("")]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(409)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create([FromBody] Users.Application.Dtos.PreferenceCreateDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }
            try
            {
                if (!new[] { "2.0", "2.1", "2.2" }.Contains(dto.WcagVersion))
                {
                    return BadRequest(new { error = $"WcagVersion inválido. Valores permitidos: 2.0, 2.1, 2.2" });
                }

                var wcagVersion = dto.WcagVersion;
                if (!Enum.TryParse<WcagLevel>(dto.WcagLevel, true, out var wcagLevel))
                {
                    return BadRequest(new { error = $"WcagLevel inválido. Valores permitidos: A, AA, AAA" });
                }

                Language language = Language.es;
                if (dto.Language is not null && !Enum.TryParse<Language>(dto.Language, true, out language))
                {
                    return BadRequest(new { error = $"Language inválido. Valores permitidos: es, en" });
                }

                VisualTheme visualTheme = VisualTheme.light;
                if (dto.VisualTheme is not null && !Enum.TryParse<VisualTheme>(dto.VisualTheme, true, out visualTheme))
                {
                    return BadRequest(new { error = $"VisualTheme inválido. Valores permitidos: light, dark" });
                }

                ReportFormat reportFormat = ReportFormat.pdf;
                if (dto.ReportFormat is not null && !Enum.TryParse<ReportFormat>(dto.ReportFormat, true, out reportFormat))
                {
                    return BadRequest(new { error = $"ReportFormat inválido. Valores permitidos: pdf, html, json, excel" });
                }

                AiResponseLevel aiResponseLevel = AiResponseLevel.intermediate;
                if (dto.AiResponseLevel is not null && !Enum.TryParse<AiResponseLevel>(dto.AiResponseLevel, true, out aiResponseLevel))
                {
                    return BadRequest(new { error = $"AiResponseLevel inválido. Valores permitidos: basic, intermediate, detailed" });
                }

                var preference = new Users.Domain.Entities.Preference
                {
                    UserId = dto.UserId,
                    WcagVersion = wcagVersion,
                    WcagLevel = wcagLevel,
                    Language = language,
                    VisualTheme = visualTheme,
                    ReportFormat = reportFormat,
                    NotificationsEnabled = dto.NotificationsEnabled ?? true,
                    AiResponseLevel = aiResponseLevel,
                    FontSize = dto.FontSize ?? 14
                };
                var created = await _preferenceService.CreatePreferenceAsync(preference);
                return Created($"/api/preference/{created.Id}", new { created.Id, message = Localization.Get("Success_PreferencesCreated", lang) });
            }
            catch (InvalidOperationException)
            {
                return Conflict(new { error = Localization.Get("Error_PreferencesExist", lang) });
            }
        }
        // DELETE: api/preferences/{id}
        /// <summary>
        /// Elimina preferencias por Id.
        /// </summary>
        /// <response code="200">Preferencias eliminadas</response>
        /// <response code="404">Preferencias no encontradas</response>
        /// <response code="401">No autenticado</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Delete(int id)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }
            var deleted = await _preferenceService.DeletePreferenceAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = Localization.Get("Error_PreferencesNotFound", lang) });
            }
            return Ok(new { message = Localization.Get("Success_PreferencesDeleted", lang) });
        }
    }
}