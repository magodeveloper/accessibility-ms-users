using Users.Api;
using Users.Api.Helpers;
using Users.Application;
using Users.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Users.Application.Services.Session;

namespace Users.Api.Controllers
{
    [ApiController]
    [Route("api/sessions")]
    public class SessionController : ControllerBase
    {
        private readonly ISessionService _sessionService;
        public SessionController(ISessionService sessionService)
        {
            _sessionService = sessionService;
        }
        // GET: api/sessions/user/{userId}
        /// <summary>
        /// Obtiene todas las sesiones por UserId.
        /// </summary>
        /// <response code="200">Sesiones encontradas</response>
        /// <response code="404">No se encontraron sesiones para el usuario</response>
        [HttpGet("user/{userId:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetByUserId(int userId)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var sessionDtos = await _sessionService.GetSessionsByUserIdAsync(userId);
            if (sessionDtos == null || !sessionDtos.Any())
                return NotFound(new { error = Localization.Get("Error_SessionNotFound", lang) });
            return Ok(new { sessions = sessionDtos, message = Localization.Get("Success_SessionFound", lang) });
        }
        // GET: api/sessions
        /// <summary>
        /// Lista todas las sesiones.
        /// </summary>
        /// <response code="200">Lista de sesiones</response>
        [HttpGet("")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetAll()
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var sessionDtos = await _sessionService.GetAllSessionsAsync();
            return Ok(new { sessions = sessionDtos, message = Localization.Get("Success_SessionFound", lang) });
        }
        // DELETE: api/sessions/{id}
        /// <summary>
        /// Elimina una sesión por Id.
        /// </summary>
        /// <response code="200">Sesión eliminada</response>
        /// <response code="404">Sesión no encontrada</response>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Delete(int id)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var deleted = await _sessionService.DeleteSessionAsync(id);
            if (!deleted)
                return NotFound(new { error = Localization.Get("Error_SessionNotFound", lang) });
            return Ok(new { message = Localization.Get("Success_SessionDeleted", lang) });
        }
        /// <summary>
        /// Elimina todas las sesiones de un usuario por su UserId.
        /// </summary>
        /// <param name="userId">Id del usuario</param>
        /// <response code="200">Sesiones eliminadas</response>
        /// <response code="404">No se encontraron sesiones para el usuario</response>
        [HttpDelete("by-user/{userId:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> DeleteByUserId(int userId)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);
            var deleted = await _sessionService.DeleteSessionsByUserIdAsync(userId);
            if (!deleted)
                return NotFound(new { error = Localization.Get("Error_SessionNotFound", lang) });
            return Ok(new { message = Localization.Get("Success_SessionDeleted", lang) });
        }
    }
}