using Users.Api;
using Users.Api.Helpers;
using Users.Application;
using Users.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Users.Application.Services.User;
using Users.Application.Services.UserContext;

namespace Users.Api.Controllers
{
    [ApiController]
    [Route("api/users")]
    [Authorize] // Requiere autenticación JWT por defecto
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IUserContext _userContext;

        public UserController(IUserService userService, IUserContext userContext)
        {
            _userService = userService;
            _userContext = userContext;
        }

        // GET: api/users/by-email?email={email}
        [HttpGet("by-email")]
        /// <summary>
        /// Obtiene un usuario por su email.
        /// </summary>
        /// <param name="email">Email del usuario a buscar.</param>
        /// <response code="200">Usuario encontrado</response>
        /// <response code="404">Usuario no encontrado</response>
        /// <response code="401">No autenticado</response>
        public async Task<IActionResult> GetByEmail([FromQuery] string email)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }

            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == email);
            if (user is null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }

            var dto = new Users.Application.Dtos.UserReadDto(user.Id, user.Nickname, user.Name, user.Lastname, user.Email, user.Role.ToString(), user.Status.ToString(), user.EmailConfirmed, user.LastLogin, user.RegistrationDate, user.CreatedAt, user.UpdatedAt);
            return Ok(new { user = dto, message = Localization.Get("Success_UserFound", lang) });
        }

        // DELETE: api/users/by-email/{email}
        [HttpDelete("by-email/{email}")]
        /// <summary>
        /// Elimina un usuario por su email.
        /// </summary>
        /// <param name="email">Email del usuario a eliminar.</param>
        /// <response code="200">Usuario eliminado</response>
        /// <response code="404">Usuario no encontrado</response>
        /// <response code="401">No autenticado</response>
        public async Task<IActionResult> DeleteByEmail(string email)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }

            var users = await _userService.GetAllUsersAsync();
            var user = users.FirstOrDefault(u => u.Email == email);
            if (user is null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }

            var deleted = await _userService.DeleteUserAsync(user.Id);
            if (!deleted)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }

            return Ok(new { message = Localization.Get("Success_UserDeleted", lang) });
        }

        // POST: api/users
        /// <summary>
        /// Crea un nuevo usuario en el sistema.
        /// </summary>
        /// <response code="201">Usuario creado exitosamente</response>
        /// <response code="409">Email o nickname ya existen</response>
        /// <response code="401">No autenticado</response>
        [HttpPost("")]
        [AllowAnonymous] // Registro público
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(409)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Create([FromBody] Users.Application.Dtos.UserCreateDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Endpoint público - no requiere autenticación (registro de nuevos usuarios)
            try
            {
                var user = new Users.Domain.Entities.User
                {
                    Nickname = dto.Nickname,
                    Name = dto.Name,
                    Lastname = dto.Lastname,
                    Email = dto.Email,
                    Password = dto.Password // El servicio se encarga de hashear
                };
                var created = await _userService.CreateUserAsync(user);
                return Created($"/api/user/{created.Id}", new { created.Id, message = Localization.Get("Success_UserCreated", lang) });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("email"))
                {
                    return Conflict(new { error = Localization.Get("Error_EmailExists", lang) });
                }

                if (ex.Message.Contains("nickname"))
                {
                    return Conflict(new { error = Localization.Get("Error_NicknameExists", lang) });
                }

                return Conflict(new { error = ex.Message });
            }
        }
        // GET: api/users/{id}
        /// <summary>
        /// Obtiene un usuario por su Id.
        /// </summary>
        /// <response code="200">Usuario encontrado</response>
        /// <response code="404">Usuario no encontrado</response>
        /// <response code="401">No autenticado</response>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> GetById(int id)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }

            var u = await _userService.GetUserByIdAsync(id);
            if (u is null)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }

            var dto = new Users.Application.Dtos.UserReadDto(u.Id, u.Nickname, u.Name, u.Lastname, u.Email, u.Role.ToString(), u.Status.ToString(), u.EmailConfirmed, u.LastLogin, u.RegistrationDate, u.CreatedAt, u.UpdatedAt);
            return Ok(new { user = dto, message = Localization.Get("Success_UserFound", lang) });
        }

        // GET: api/users
        /// <summary>
        /// Lista todos los usuarios.
        /// </summary>
        /// <response code="200">Lista de usuarios</response>
        [HttpGet("")]
        [AllowAnonymous] // Endpoint público
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetAll()
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            var users = await _userService.GetAllUsersAsync();
            var list = users.Select(u => new Users.Application.Dtos.UserReadDto(u.Id, u.Nickname, u.Name, u.Lastname, u.Email, u.Role.ToString(), u.Status.ToString(), u.EmailConfirmed, u.LastLogin, u.RegistrationDate, u.CreatedAt, u.UpdatedAt)).ToList();
            return Ok(new { users = list, message = Localization.Get("Success_ListUsers", lang) });
        }

        // DELETE: api/users/{id}
        /// <summary>
        /// Elimina un usuario por su Id.
        /// </summary>
        /// <response code="200">Usuario eliminado</response>
        /// <response code="404">Usuario no encontrado</response>
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

            var deleted = await _userService.DeleteUserAsync(id);
            if (!deleted)
            {
                return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
            }

            return Ok(new { message = Localization.Get("Success_UserDeleted", lang) });
        }

        // PATCH: api/users/{id}
        /// <summary>
        /// Actualiza un usuario existente (parcial).
        /// </summary>
        /// <param name="id">Id del usuario a actualizar.</param>
        /// <param name="dto">Datos a actualizar del usuario.</param>
        /// <response code="200">Usuario actualizado exitosamente</response>
        /// <response code="404">Usuario no encontrado</response>
        /// <response code="409">Email o nickname ya existen</response>
        /// <response code="401">No autenticado</response>
        [HttpPatch("{id:int}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> Update(int id, [FromBody] Users.Application.Dtos.UserPatchDto dto)
        {
            var lang = LanguageHelper.GetRequestLanguage(Request);

            // Validar autenticación
            if (!_userContext.IsAuthenticated)
            {
                return Unauthorized(new { error = Localization.Get("Error_NotAuthenticated", lang) });
            }

            try
            {
                var updated = await _userService.UpdateUserAsync(id, dto);
                if (updated == null)
                {
                    return NotFound(new { error = Localization.Get("Error_UserNotFound", lang) });
                }

                var userDto = new Users.Application.Dtos.UserReadDto(
                    updated.Id, updated.Nickname, updated.Name, updated.Lastname,
                    updated.Email, updated.Role.ToString(), updated.Status.ToString(),
                    updated.EmailConfirmed, updated.LastLogin, updated.RegistrationDate,
                    updated.CreatedAt, updated.UpdatedAt);

                return Ok(new { user = userDto, message = Localization.Get("Success_UserUpdated", lang) });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("email"))
                {
                    return Conflict(new { error = Localization.Get("Error_EmailExists", lang) });
                }

                if (ex.Message.Contains("nickname"))
                {
                    return Conflict(new { error = Localization.Get("Error_NicknameExists", lang) });
                }

                return Conflict(new { error = ex.Message });
            }
        }
    }
}