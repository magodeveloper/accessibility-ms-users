using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Users.Infrastructure;
using Users.Application;
using Users.Domain;
using static Users.Api.Localization;
// (usings duplicados eliminados)

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(); // .NET 9
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();


builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPasswordService, BcryptPasswordService>();
builder.Services.AddSingleton<ISessionTokenService, SessionTokenService>();

// FluentValidation: registra todos los validadores del ensamblado
builder.Services.AddValidatorsFromAssemblyContaining<UserCreateDtoValidator>();

// Configuración simple para expiración del token
var tokenMinutes = builder.Configuration.GetValue<int?>("Auth:SessionTokenMinutes") ?? 1440;

var app = builder.Build();

// Middleware global para manejo de errores uniformes
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        var exceptionHandlerPathFeature = context.Features.Get<IExceptionHandlerPathFeature>();
        var error = exceptionHandlerPathFeature?.Error;
        var result = JsonSerializer.Serialize(new { error = error?.Message });
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(result);
    });
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Grupo v1
var api = app.MapGroup("/api/v1");


// Tag constants
const string UsersTag = "Users";
const string JsonContentType = "application/json";

// Helper para obtener el idioma desde el header Accept-Language
string GetLang(HttpContext context)
    => context.Request.Headers["Accept-Language"].FirstOrDefault()?.Split(',')[0]?.Substring(0, 2) ?? "es";

// USERS
api.MapPost("/users", async (UsersDbContext db, IPasswordService pwd, UserCreateDto dto, HttpContext context) =>
{
    var lang = GetLang(context);
    if (await db.Users.AnyAsync(u => u.Email == dto.Email)) return Results.Conflict(Get("Error_EmailExists", lang));
    if (await db.Users.AnyAsync(u => u.Nickname == dto.Nickname)) return Results.Conflict(Get("Error_NicknameExists", lang));

    var now = DateTime.UtcNow;
    var user = new User
    {
        Nickname = dto.Nickname,
        Name = dto.Name,
        Lastname = dto.Lastname,
        Email = dto.Email,
        Password = pwd.Hash(dto.Password),
        Role = UserRole.user,
        Status = UserStatus.active,
        EmailConfirmed = false,
        RegistrationDate = now,
        CreatedAt = now,
        UpdatedAt = now
    };
    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Created($"/api/v1/users/{user.Id}", new { user.Id, message = Get("Success_UserCreated", lang) });
})
    .WithTags(UsersTag)
    .WithSummary("Crear usuario")
    .WithDescription("Crea un nuevo usuario en el sistema. Retorna el Id del usuario creado. Retorna 409 si el email o nickname ya existen.")
    .Produces(201, typeof(object), JsonContentType)
    .Produces(409, typeof(string), JsonContentType)
    .Produces(400)
    .WithOpenApi(op =>
    {
        op.RequestBody = new Microsoft.OpenApi.Models.OpenApiRequestBody
        {
            Description = "Datos del usuario a crear",
            Content = {
                [JsonContentType] = new Microsoft.OpenApi.Models.OpenApiMediaType {
                    // Example reemplazado por OpenApiObject manual
                    Example = new OpenApiObject {
                        ["nickname"] = new OpenApiString("usuario1"),
                        ["name"] = new OpenApiString("Juan"),
                        ["lastname"] = new OpenApiString("Pérez"),
                        ["email"] = new OpenApiString("juan@email.com"),
                        ["password"] = new OpenApiString("12345678")
                    }
                }
            }
        };
        op.Responses["201"].Description = "Usuario creado exitosamente";
        op.Responses["409"] = new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Email o nickname ya existen" };
        op.Responses["400"] = new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Datos inválidos" };
        return op;
    });

api.MapGet("/users", async (UsersDbContext db, HttpContext context) =>
{
    var lang = GetLang(context);
    var list = await db.Users
        .Select(u => new UserReadDto(u.Id, u.Nickname, u.Name, u.Lastname, u.Email, u.Role.ToString(), u.Status.ToString(), u.EmailConfirmed, u.LastLogin, u.RegistrationDate, u.CreatedAt, u.UpdatedAt))
        .ToListAsync();
    return Results.Ok(new { users = list, message = Get("Success_ListUsers", lang) });
})
    .WithTags(UsersTag)
    .WithSummary("Listar usuarios")
    .WithDescription("Obtiene la lista de todos los usuarios registrados.")
    .Produces(200, typeof(List<UserReadDto>), JsonContentType)
    .WithOpenApi(op =>
    {
        op.Responses["200"].Description = "Lista de usuarios";
        return op;
    });

api.MapGet("/users/{id:int}", async (UsersDbContext db, int id, HttpContext context) =>
{
    var lang = GetLang(context);
    var u = await db.Users.FindAsync(id);
    return u is null
    ? Results.NotFound(Get("Error_UserNotFound", lang))
    : Results.Ok(new { user = new UserReadDto(u.Id, u.Nickname, u.Name, u.Lastname, u.Email, u.Role.ToString(), u.Status.ToString(), u.EmailConfirmed, u.LastLogin, u.RegistrationDate, u.CreatedAt, u.UpdatedAt), message = Get("Success_UserFound", lang) });
})
    .WithTags(UsersTag)
    .WithSummary("Obtener usuario por Id")
    .WithDescription("Obtiene un usuario por su identificador único. Retorna 404 si no existe.")
    .Produces(200, typeof(UserReadDto), JsonContentType)
    .Produces(404)
    .WithOpenApi(op =>
    {
        op.Responses["200"].Description = "Usuario encontrado";
        op.Responses["404"] = new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Usuario no encontrado" };
        return op;
    });

api.MapGet("/users/by-email", async (UsersDbContext db, string email, HttpContext context) =>
{
    var lang = GetLang(context);
    var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
    return u is null
    ? Results.NotFound(Get("Error_UserNotFound", lang))
    : Results.Ok(new { u.Id, u.Email, u.Nickname, u.Name, u.Lastname, Role = u.Role.ToString(), Status = u.Status.ToString(), u.EmailConfirmed, message = Get("Success_UserFound", lang) });
}).WithTags(UsersTag).WithSummary("Obtener usuario por email");

api.MapPatch("/users/{id:int}", async (UsersDbContext db, int id, UserPatchDto dto, HttpContext context) =>
{
    var lang = GetLang(context);
    var u = await db.Users.FindAsync(id);
    if (u is null) return Results.NotFound(Get("Error_UserNotFound", lang));

    if (dto.Nickname is not null) u.Nickname = dto.Nickname;
    if (dto.Name is not null) u.Name = dto.Name;
    if (dto.Lastname is not null) u.Lastname = dto.Lastname;
    if (dto.Role is not null && Enum.TryParse<UserRole>(dto.Role, out var r)) u.Role = r;
    if (dto.Status is not null && Enum.TryParse<UserStatus>(dto.Status, out var s)) u.Status = s;
    if (dto.EmailConfirmed.HasValue) u.EmailConfirmed = dto.EmailConfirmed.Value;

    u.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = Get("Success_UserUpdated", lang) });
}).WithTags(UsersTag).WithSummary("Actualizar parcialmente usuario");

api.MapDelete("/users/{id:int}", async (UsersDbContext db, int id, HttpContext context) =>
{
    var lang = GetLang(context);
    var u = await db.Users.FindAsync(id);
    if (u is null) return Results.NotFound(Get("Error_UserNotFound", lang));
    db.Users.Remove(u);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = Get("Success_UserDeleted", lang) });
}).WithTags(UsersTag).WithSummary("Eliminar usuario");

// AUTH (login, reset/restore, confirm email)
api.MapPost("/auth/login", async (UsersDbContext db, IPasswordService pwd, ISessionTokenService tok, LoginDto dto, HttpContext context) =>

{
    var lang = GetLang(context);
    var u = await db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
    if (u is null) return Results.Unauthorized();

    if (u.Status != UserStatus.active)
        return Results.Json(new { error = Get("Error_UserInactive", lang) }, statusCode: 403);
    if (!pwd.Verify(dto.Password, u.Password)) return Results.Unauthorized();

    u.LastLogin = DateTime.UtcNow;
    var (token, tokenHash) = tok.GenerateToken();
    var session = new Users.Domain.Session
    {
        UserId = u.Id,
        TokenHash = tokenHash,
        CreatedAt = DateTime.UtcNow,
        ExpiresAt = DateTime.UtcNow.AddMinutes(tokenMinutes)
    };
    db.Sessions.Add(session);
    await db.SaveChangesAsync();

    return Results.Ok(new { token, expiresAt = session.ExpiresAt, message = Get("Success_Login", lang) });
})
    .WithTags("Auth")
    .WithSummary("Autenticar usuario y crear sesión")
    .WithDescription("Autentica un usuario y crea una sesión. Retorna un token de sesión si las credenciales son válidas.")
    .Produces(200, typeof(LoginResponseDto), JsonContentType)
    .Produces(401)
    .Produces(403)
    .Produces(400)
    .WithOpenApi(op =>
    {
        op.RequestBody = new Microsoft.OpenApi.Models.OpenApiRequestBody
        {
            Description = "Credenciales de login",
            Content = {
                [JsonContentType] = new Microsoft.OpenApi.Models.OpenApiMediaType {
                    // Example reemplazado por OpenApiObject manual
                    Example = new OpenApiObject {
                        ["email"] = new OpenApiString("juan@email.com"),
                        ["password"] = new OpenApiString("12345678")
                    }
                }
            }
        };
        op.Responses["200"].Description = "Login exitoso, retorna token";
        op.Responses["401"] = new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Credenciales inválidas" };
        op.Responses["403"] = new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Usuario inactivo o prohibido" };
        op.Responses["400"] = new Microsoft.OpenApi.Models.OpenApiResponse { Description = "Datos inválidos" };
        return op;
    });

api.MapPost("/auth/reset-password", async (UsersDbContext db, IPasswordService pwd, ResetPasswordDto dto, HttpContext context) =>
{
    var lang = GetLang(context);
    var u = await db.Users.FirstOrDefaultAsync(x => x.Email == dto.Email);
    if (u is null) return Results.NotFound(Get("Error_UserNotFound", lang));

    u.Password = pwd.Hash(dto.NewPassword);
    u.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = Get("Success_PasswordReset", lang) });
}).WithTags("Auth").WithSummary("Resetear contraseña (demo simple)");

api.MapPost("/auth/confirm-email/{userId:int}", async (UsersDbContext db, int userId, HttpContext context) =>
{
    var lang = GetLang(context);
    var u = await db.Users.FindAsync(userId);
    if (u is null) return Results.NotFound(Get("Error_UserNotFound", lang));
    u.EmailConfirmed = true;
    u.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = Get("Success_EmailConfirmed", lang) });
}).WithTags("Auth").WithSummary("Confirmar email (demo)");

// SESSIONS CRUD (básico)
api.MapGet("/sessions/{id:int}", async (UsersDbContext db, int id, HttpContext context) =>
{
    var lang = GetLang(context);
    var s = await db.Sessions.Include(x => x.User).FirstOrDefaultAsync(x => x.Id == id);
    return s is null ? Results.NotFound(Get("Error_SessionNotFound", lang)) : Results.Ok(new { s.Id, s.UserId, s.CreatedAt, s.ExpiresAt, message = Get("Success_SessionFound", lang) });
}).WithTags("Sessions").WithSummary("Consultar sesión por Id");

api.MapDelete("/sessions/{id:int}", async (UsersDbContext db, int id, HttpContext context) =>
{
    var lang = GetLang(context);
    var s = await db.Sessions.FindAsync(id);
    if (s is null) return Results.NotFound(Get("Error_SessionNotFound", lang));
    db.Sessions.Remove(s);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = Get("Success_SessionDeleted", lang) });
}).WithTags("Sessions").WithSummary("Eliminar sesión (logout puntual)");

// PREFERENCES
api.MapPost("/preferences", async (UsersDbContext db, PreferenceCreateDto dto, HttpContext context) =>
{
    var lang = GetLang(context);
    // evitar duplicados por user_id
    var exists = await db.Preferences.AnyAsync(p => p.UserId == dto.UserId);
    if (exists) return Results.Conflict(Get("Error_PreferencesExist", lang));

    var now = DateTime.UtcNow;
    var p = new Preference
    {
        UserId = dto.UserId,
        WcagVersion = dto.WcagVersion switch { "2.0" => WcagVersion._2_0, "2.1" => WcagVersion._2_1, _ => WcagVersion._2_2 },
        WcagLevel = Enum.Parse<WcagLevel>(dto.WcagLevel),
        Language = Enum.TryParse<Language>(dto.Language ?? "es", out var langPref) ? langPref : Language.es,
        VisualTheme = Enum.TryParse<VisualTheme>(dto.VisualTheme ?? "light", out var theme) ? theme : VisualTheme.light,
        ReportFormat = Enum.TryParse<ReportFormat>(dto.ReportFormat ?? "pdf", out var fmt) ? fmt : ReportFormat.pdf,
        NotificationsEnabled = dto.NotificationsEnabled ?? true,
        AiResponseLevel = Enum.TryParse<AiResponseLevel>(dto.AiResponseLevel ?? "intermediate", out var ai) ? ai : AiResponseLevel.intermediate,
        FontSize = dto.FontSize ?? 14,
        CreatedAt = now,
        UpdatedAt = now
    };
    db.Preferences.Add(p);
    await db.SaveChangesAsync();
    return Results.Created($"/api/v1/preferences/{p.Id}", new { p.Id, message = Get("Success_PreferencesCreated", lang) });
}).WithTags("Preferences").WithSummary("Crear preferencias");

api.MapGet("/preferences/by-user/{userId:int}", async (UsersDbContext db, int userId, HttpContext context) =>
{
    var lang = GetLang(context);
    var p = await db.Preferences.FirstOrDefaultAsync(x => x.UserId == userId);
    return p is null ? Results.NotFound(Get("Error_PreferencesNotFound", lang)) : Results.Ok(new { preferences = p, message = Get("Success_PreferencesFound", lang) });
}).WithTags("Preferences").WithSummary("Obtener preferencias por UserId");

api.MapPatch("/preferences/{id:int}", async (UsersDbContext db, int id, PreferencePatchDto dto, HttpContext context) =>
{
    var lang = GetLang(context);
    var p = await db.Preferences.FindAsync(id);
    if (p is null) return Results.NotFound(Get("Error_PreferencesNotFound", lang));

    if (!string.IsNullOrEmpty(dto.WcagVersion))
        p.WcagVersion = dto.WcagVersion switch { "2.0" => WcagVersion._2_0, "2.1" => WcagVersion._2_1, _ => WcagVersion._2_2 };
    if (!string.IsNullOrEmpty(dto.WcagLevel) && Enum.TryParse<WcagLevel>(dto.WcagLevel, out var lvl)) p.WcagLevel = lvl;
    if (!string.IsNullOrEmpty(dto.Language) && Enum.TryParse<Language>(dto.Language, out var langPref)) p.Language = langPref;
    if (!string.IsNullOrEmpty(dto.VisualTheme) && Enum.TryParse<VisualTheme>(dto.VisualTheme, out var theme)) p.VisualTheme = theme;
    if (!string.IsNullOrEmpty(dto.ReportFormat) && Enum.TryParse<ReportFormat>(dto.ReportFormat, out var fmt)) p.ReportFormat = fmt;
    if (dto.NotificationsEnabled.HasValue) p.NotificationsEnabled = dto.NotificationsEnabled.Value;
    if (!string.IsNullOrEmpty(dto.AiResponseLevel) && Enum.TryParse<AiResponseLevel>(dto.AiResponseLevel, out var ai)) p.AiResponseLevel = ai;
    if (dto.FontSize.HasValue) p.FontSize = dto.FontSize.Value;

    p.UpdatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.Ok(new { message = Get("Success_PreferencesUpdated", lang) });
}).WithTags("Preferences").WithSummary("Actualizar parcialmente preferencias");

app.Run();

// Necesario para tests de integración (WebApplicationFactory)
public partial class Program { }