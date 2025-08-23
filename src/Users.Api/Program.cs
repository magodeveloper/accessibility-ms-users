using System.Linq;
using System.Text.Json;
using FluentValidation;
using Users.Infrastructure;
using Microsoft.OpenApi.Any;
using Users.Application.Dtos;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Users.Infrastructure.Data;
using Users.Application.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Users.Application.Services.User;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Users.Application.Services.Session;
using Users.Application.Services.Preference;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(); // .NET 9

// Registrar controladores MVC
builder.Services.AddControllers(); // Controladores

builder.Services.AddInfrastructure(builder.Configuration); // Infraestructura
builder.Services.AddScoped<IPasswordService, BcryptPasswordService>(); // Servicio de contraseñas
builder.Services.AddSingleton<ISessionTokenService, SessionTokenService>(); // Servicio de tokens de sesión

// Servicios de dominio
builder.Services.AddScoped<IUserService, UserService>(); // Servicio de usuarios
builder.Services.AddScoped<IPreferenceService, PreferenceService>(); // Servicio de preferencias
builder.Services.AddScoped<ISessionService, SessionService>(); // Servicio de sesiones

// FluentValidation: registra todos los validadores del ensamblado
builder.Services.AddValidatorsFromAssemblyContaining<UserCreateDtoValidator>(); // Validadores

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer(); // Explorador de API
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Users API",
        Version = "v1"
    });
});

// Controllers
builder.Services.AddControllers() // Controladores
    .AddDataAnnotationsLocalization() // Localización de anotaciones de datos
    .AddViewLocalization(); // Localización de vistas

var app = builder.Build(); // Construcción de la aplicación

// Migración automática de la base de datos al iniciar la API - solo en producción/desarrollo
var environment = app.Environment.EnvironmentName;
if (environment != "TestEnvironment")
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await db.Database.MigrateAsync();
    }
}
else
{
    // Para tests, solo asegurar que la base de datos se cree
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}

var supportedCultures = new[] { "es", "en" };
var localizationOptions = new RequestLocalizationOptions()
    .SetDefaultCulture("es")
    .AddSupportedCultures(supportedCultures)
    .AddSupportedUICultures(supportedCultures);
app.UseRequestLocalization(localizationOptions);

// Middleware global para manejo de errores uniformes
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        // Detectar idioma desde el header Accept-Language o default 'es'
        var lang = context.Request.Headers["Accept-Language"].FirstOrDefault()?.Split(',')[0] ?? "es";
        string Get(string key, string lang) => Users.Application.Localization.Get(key, lang);
        var result = JsonSerializer.Serialize(new { error = Get("Error_InternalServer", lang) });
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(result);
    });
});

// app.UseHttpsRedirection();

// app.UseAuthorization();

// Habilitar el enrutamiento de controladores
app.MapControllers();

// Swagger/OpenAPI
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.RunAsync(); // Ejecución de la aplicación

// Necesario para tests de integración (WebApplicationFactory)
namespace Users.Api
{
    public partial class Program { protected Program() { } }
}