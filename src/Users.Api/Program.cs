using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Any;
using Microsoft.Extensions.Localization;
using Microsoft.OpenApi.Models;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Users.Infrastructure;
using Users.Application;
using Users.Domain;
using static Users.Api.Localization;

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
        // Detectar idioma desde el header Accept-Language o default 'es'
        var lang = context.Request.Headers["Accept-Language"].FirstOrDefault()?.Split(',')[0] ?? "es";
        string Get(string key, string lang) => Users.Api.Localization.Get(key, lang); // Usa tu helper de localización
        var result = JsonSerializer.Serialize(new { error = Get("Error_InternalServer", lang) });
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(result);
    });
});
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();

    // Grupo v1
    var api = app.MapGroup("/api/v1");

    // Constantes globales para tags y errores
    const string UsersTag = "Users";
    const string JsonContentType = "application/json";
    const string ErrorUserNotFound = "Error_UserNotFound";
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

    // Endpoint combinado: crear usuario y preferencias por defecto
    api.MapPost("/users-with-preferences", async (
        UserCreateDto userDto,
        UsersDbContext db,
        IPasswordService passwordService,
        HttpContext context) =>
    {
        var lang = GetLang(context);
        if (await db.Users.AnyAsync(u => u.Email == userDto.Email))
            return Results.Conflict(Get("Error_EmailExists", lang));
        if (await db.Users.AnyAsync(u => u.Nickname == userDto.Nickname))
            return Results.Conflict(Get("Error_NicknameExists", lang));

        var now = DateTime.UtcNow;
        var user = new User
        {
            Nickname = userDto.Nickname,
            Name = userDto.Name,
            Lastname = userDto.Lastname,
            Email = userDto.Email,
            Password = passwordService.Hash(userDto.Password),
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = now,
            CreatedAt = now,
            UpdatedAt = now
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();

        // Preferencias por defecto
        var pref = new Preference
        {
            UserId = user.Id,
            WcagVersion = WcagVersion._2_1,
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
        db.Preferences.Add(pref);
        await db.SaveChangesAsync();

        string WcagVersionToString(WcagVersion v) => v switch
        {
            WcagVersion._2_0 => "2.0",
            WcagVersion._2_1 => "2.1",
            WcagVersion._2_2 => "2.2",
            _ => v.ToString()
        };
        string AiResponseLevelToString(AiResponseLevel a) => a switch
        {
            AiResponseLevel.basic => "basic",
            AiResponseLevel.intermediate => "intermediate",
            AiResponseLevel.detailed => "detailed",
            _ => a.ToString()
        };
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
                wcagVersion = WcagVersionToString(pref.WcagVersion),
                wcagLevel = pref.WcagLevel.ToString(),
                language = pref.Language.ToString(),
                visualTheme = pref.VisualTheme.ToString(),
                reportFormat = pref.ReportFormat.ToString(),
                pref.NotificationsEnabled,
                aiResponseLevel = AiResponseLevelToString(pref.AiResponseLevel),
                pref.FontSize
            }
        };
        return Results.Created($"/api/v1/users-with-preferences/{user.Id}", result);
    })
        .WithTags(UsersTag)
        .WithSummary("Crear usuario y preferencias por defecto")
        .WithDescription("Crea un usuario y sus preferencias por defecto en una sola llamada. Retorna 409 si el email o nickname ya existen.")
        .Produces(201, typeof(object), JsonContentType)
        .Produces(409, typeof(string), JsonContentType)
        .Produces(400)
        .WithOpenApi();

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
        ? Results.NotFound(Get(ErrorUserNotFound, lang))
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
        ? Results.NotFound(Get(ErrorUserNotFound, lang))
        : Results.Ok(new { u.Id, u.Email, u.Nickname, u.Name, u.Lastname, Role = u.Role.ToString(), Status = u.Status.ToString(), u.EmailConfirmed, message = Get("Success_UserFound", lang) });
    }).WithTags(UsersTag).WithSummary("Obtener usuario por email");

    api.MapPatch("/users/by-email/{email}", async (UsersDbContext db, string email, UserPatchDto dto, HttpContext context) =>
    {
        var lang = GetLang(context);
        var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null) return Results.NotFound(Get(ErrorUserNotFound, lang));

        // Validar email único si se quiere actualizar
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != u.Email)
        {
            var exists = await db.Users.AnyAsync(x => x.Email == dto.Email && x.Id != u.Id);
            if (exists) return Results.Conflict(Get("Error_EmailExists", lang));
            u.Email = dto.Email;
        }

        if (dto.Password is not null)
        {
            var pwd = context.RequestServices.GetRequiredService<IPasswordService>();
            u.Password = pwd.Hash(dto.Password);
        }
        if (dto.Nickname is not null) u.Nickname = dto.Nickname;
        if (dto.Name is not null) u.Name = dto.Name;
        if (dto.Lastname is not null) u.Lastname = dto.Lastname;
        if (dto.Role is not null && Enum.TryParse<UserRole>(dto.Role, out var r)) u.Role = r;
        if (dto.Status is not null && Enum.TryParse<UserStatus>(dto.Status, out var s)) u.Status = s;
        if (dto.EmailConfirmed.HasValue) u.EmailConfirmed = dto.EmailConfirmed.Value;

        u.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new { message = Get("Success_UserUpdated", lang) });
    }).WithTags(UsersTag).WithSummary("Actualizar parcialmente usuario por email");

    api.MapDelete("/users/by-email/{email}", async (UsersDbContext db, string email, HttpContext context) =>
    {
        var lang = GetLang(context);
        var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null) return Results.NotFound(Get(ErrorUserNotFound, lang));
        db.Users.Remove(u);
        await db.SaveChangesAsync();
        return Results.Ok(new { message = Get("Success_UserDeleted", lang) });
    }).WithTags(UsersTag).WithSummary("Eliminar usuario por email");

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
        if (u is null) return Results.NotFound(Get(ErrorUserNotFound, lang));

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

    api.MapGet("/preferences/by-user/{email}", async (UsersDbContext db, string email, HttpContext context) =>
    {
        var lang = GetLang(context);
        var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null)
            return Results.NotFound(Get(ErrorUserNotFound, lang));
        var p = await db.Preferences.Include(p => p.User).FirstOrDefaultAsync(x => x.UserId == u.Id);
        if (p is null)
            return Results.NotFound(Get("Error_PreferencesNotFound", lang));

        string WcagVersionToString(WcagVersion v) => v switch
        {
            WcagVersion._2_0 => "2.0",
            WcagVersion._2_1 => "2.1",
            WcagVersion._2_2 => "2.2",
            _ => v.ToString()
        };
        string AiResponseLevelToString(AiResponseLevel a) => a switch
        {
            AiResponseLevel.basic => "basic",
            AiResponseLevel.intermediate => "intermediate",
            AiResponseLevel.detailed => "detailed",
            _ => a.ToString()
        };
        var preferences = new
        {
            p.Id,
            p.UserId,
            wcagVersion = WcagVersionToString(p.WcagVersion),
            wcagLevel = p.WcagLevel.ToString(),
            language = p.Language.ToString(),
            visualTheme = p.VisualTheme.ToString(),
            reportFormat = p.ReportFormat.ToString(),
            p.NotificationsEnabled,
            aiResponseLevel = AiResponseLevelToString(p.AiResponseLevel),
            p.FontSize,
            p.CreatedAt,
            p.UpdatedAt,
            user = p.User == null ? null : new
            {
                p.User.Id,
                p.User.Nickname,
                p.User.Name,
                p.User.Lastname,
                p.User.Email,
                Role = p.User.Role.ToString(),
                Status = p.User.Status.ToString(),
                p.User.EmailConfirmed,
                p.User.LastLogin,
                p.User.RegistrationDate,
                p.User.CreatedAt,
                p.User.UpdatedAt
            }
        };
        return Results.Ok(new { preferences, message = Get("Success_PreferencesFound", lang) });
    }).WithTags("Preferences").WithSummary("Obtener preferencias por email de usuario");


    api.MapPatch("/preferences/by-user/{email}", async (UsersDbContext db, string email, PreferenceUserPatchDto dto, HttpContext context) =>
    {
        var lang = GetLang(context);
        var u = await db.Users.FirstOrDefaultAsync(x => x.Email == email);
        if (u is null) return Results.NotFound(Get(ErrorUserNotFound, lang));

        // Solo debe existir un registro de preferencias por usuario
        var p = await db.Preferences.FirstOrDefaultAsync(x => x.UserId == u.Id);
        if (p is null) return Results.NotFound(Get("Error_PreferencesNotFound", lang));

        // Actualizar preferencias
        if (!string.IsNullOrEmpty(dto.WcagVersion))
            p.WcagVersion = dto.WcagVersion switch { "2.0" => WcagVersion._2_0, "2.1" => WcagVersion._2_1, _ => WcagVersion._2_2 };
        if (!string.IsNullOrEmpty(dto.WcagLevel) && Enum.TryParse<WcagLevel>(dto.WcagLevel, out var lvl)) p.WcagLevel = lvl;
        if (!string.IsNullOrEmpty(dto.Language) && Enum.TryParse<Language>(dto.Language, out var langPref)) p.Language = langPref;
        if (!string.IsNullOrEmpty(dto.VisualTheme) && Enum.TryParse<VisualTheme>(dto.VisualTheme, out var theme)) p.VisualTheme = theme;
        if (!string.IsNullOrEmpty(dto.ReportFormat) && Enum.TryParse<ReportFormat>(dto.ReportFormat, out var fmt)) p.ReportFormat = fmt;
        if (dto.NotificationsEnabled.HasValue) p.NotificationsEnabled = dto.NotificationsEnabled.Value;
        if (!string.IsNullOrEmpty(dto.AiResponseLevel) && Enum.TryParse<AiResponseLevel>(dto.AiResponseLevel, out var ai)) p.AiResponseLevel = ai;
        if (dto.FontSize.HasValue) p.FontSize = dto.FontSize.Value;

        // Actualizar usuario
        if (!string.IsNullOrEmpty(dto.Email) && dto.Email != u.Email)
        {
            var exists = await db.Users.AnyAsync(x => x.Email == dto.Email && x.Id != u.Id);
            if (exists) return Results.Conflict(Get("Error_EmailExists", lang));
            u.Email = dto.Email;
        }
        if (dto.Password is not null)
        {
            var pwd = context.RequestServices.GetRequiredService<IPasswordService>();
            u.Password = pwd.Hash(dto.Password);
        }
        if (dto.Nickname is not null) u.Nickname = dto.Nickname;
        if (dto.Name is not null) u.Name = dto.Name;
        if (dto.Lastname is not null) u.Lastname = dto.Lastname;
        if (dto.Role is not null && Enum.TryParse<UserRole>(dto.Role, out var r)) u.Role = r;
        if (dto.Status is not null && Enum.TryParse<UserStatus>(dto.Status, out var s)) u.Status = s;
        if (dto.EmailConfirmed.HasValue) u.EmailConfirmed = dto.EmailConfirmed.Value;

        p.UpdatedAt = DateTime.UtcNow;
        u.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return Results.Ok(new { message = Get("Success_PreferencesAndUserUpdated", lang) });

    }).WithTags("Preferences").WithSummary("Actualizar preferencias y usuario en una sola llamada por email");

    // --- Fin de los endpoints ---
    // --- Fin de los endpoints ---
}
await app.RunAsync();

// Necesario para tests de integración (WebApplicationFactory)
namespace Users.Api
{
    public partial class Program { protected Program() { } }
}