using System.Linq;
using Prometheus;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Users.Api.Services;
using Users.Api.Middleware;
using Users.Infrastructure;
using Microsoft.OpenApi.Any;
using Users.Api.HealthChecks;
using Users.Application.Dtos;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Http;
using Users.Infrastructure.Data;
using Users.Application.Services;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using Microsoft.IdentityModel.Tokens;
using Users.Application.Services.User;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Users.Application.Services.Session;
using Users.Application.Services.Preference;
using Users.Application.Services.UserContext;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi(); // .NET 9

// Registrar controladores MVC
builder.Services.AddControllers(); // Controladores

builder.Services.AddInfrastructure(builder.Configuration); // Infraestructura
builder.Services.AddScoped<IPasswordService, BcryptPasswordService>(); // Servicio de contraseñas
builder.Services.AddSingleton<ISessionTokenService, SessionTokenService>(); // Servicio de tokens de sesión (legacy)
builder.Services.AddSingleton<IJwtTokenService, JwtTokenService>(); // Servicio JWT (nuevo)

// Servicios de dominio
builder.Services.AddScoped<IUserService, UserService>(); // Servicio de usuarios
builder.Services.AddScoped<IPreferenceService, PreferenceService>(); // Servicio de preferencias
builder.Services.AddScoped<ISessionService, SessionService>(); // Servicio de sesiones

// Registrar UserContext como scoped para inyección en controllers
builder.Services.AddScoped<IUserContext, UserContext>();

// Servicio de métricas
builder.Services.AddSingleton<IUserMetricsService, UserMetricsService>();

// FluentValidation: registra todos los validadores del ensamblado
builder.Services.AddValidatorsFromAssemblyContaining<UserCreateDtoValidator>(); // Validadores
builder.Services.AddFluentValidationAutoValidation(); // Auto-validación

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer(); // Explorador de API
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Users API",
        Version = "v1",
        Description = "API de gestión de usuarios con autenticación JWT"
    });

    // Configuración de seguridad JWT Bearer
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Ingrese el token JWT en el formato: Bearer {token}"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Controllers
builder.Services.AddControllers() // Controladores
    .AddDataAnnotationsLocalization() // Localización de anotaciones de datos
    .AddViewLocalization(); // Localización de vistas

// --- JWT Authentication Configuration ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];
var issuer = jwtSettings["Issuer"];
var audience = jwtSettings["Audience"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("JwtSettings:SecretKey is required");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// --- Health Checks Configuration ---
var healthChecksConfig = builder.Configuration.GetSection("HealthChecks");
var memoryThresholdMB = healthChecksConfig.GetValue<long>("MemoryThresholdMB", 512);
var memoryThresholdBytes = memoryThresholdMB * 1024L * 1024L;

// Registrar health checks como servicios
builder.Services.AddSingleton<IHealthCheck>(sp =>
    new MemoryHealthCheck(
        sp.GetRequiredService<ILogger<MemoryHealthCheck>>(),
        memoryThresholdBytes));

var healthChecksBuilder = builder.Services.AddHealthChecks()
    // Health check básico de la aplicación
    .AddCheck<ApplicationHealthCheck>(
        "application",
        tags: new[] { "live", "ready" })

    // Health check de memoria  
    .AddCheck<MemoryHealthCheck>(
        "memory",
        tags: new[] { "live" })

    // Health check de base de datos personalizado
    .AddCheck<DatabaseHealthCheck>(
        "database",
        tags: new[] { "ready" })

    // Health check de EF Core
    .AddDbContextCheck<UsersDbContext>(
        "users_dbcontext",
        tags: new[] { "ready" });

// Health check de MySQL (opcional, requiere connection string)
var connectionString = builder.Configuration.GetConnectionString("Default");
if (!string.IsNullOrEmpty(connectionString))
{
    healthChecksBuilder.AddMySql(
        connectionString,
        name: "mysql",
        tags: new[] { "ready", "database" });
}

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

// --- Prometheus Metrics ---
// Configurar métricas HTTP automáticas
var metricsConfig = builder.Configuration.GetSection("Metrics");
var metricsEnabled = metricsConfig.GetValue<bool>("Enabled", true);

if (metricsEnabled)
{
    // Usar middleware de Prometheus para métricas HTTP
    app.UseHttpMetrics(options =>
    {
        // Personalizar rutas para evitar alta cardinalidad
        options.ReduceStatusCodeCardinality();

        // Agregar información de ruta sin parámetros dinámicos
        options.AddCustomLabel("endpoint", context =>
        {
            var endpoint = context.GetEndpoint();
            return endpoint?.DisplayName ?? "unknown";
        });
    });
}

// Gateway Secret Validation - Valida que las peticiones vengan del Gateway
app.UseGatewaySecretValidation();

// Habilitar autenticación y autorización JWT (debe ir ANTES del UserContextMiddleware)
app.UseAuthentication();
app.UseAuthorization();

// Registrar middleware para extraer user context desde headers X-User-* o JWT claims
app.UseMiddleware<UserContextMiddleware>();

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

// Habilitar el enrutamiento de controladores
app.MapControllers();

// --- Health Check Endpoints ---
// Endpoint de liveness: verifica que la aplicación está viva (no verifica dependencias)
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Endpoint de readiness: verifica que la aplicación está lista (incluye DB y dependencias)
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                data = e.Value.Data,
                exception = e.Value.Exception?.Message
            })
        });
        await context.Response.WriteAsync(result);
    }
});

// Endpoint general de health (incluye todos los checks)
app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            timestamp = DateTime.UtcNow,
            totalDuration = report.TotalDuration.TotalMilliseconds,
            entries = report.Entries.ToDictionary(
                e => e.Key,
                e => new
                {
                    status = e.Value.Status.ToString(),
                    description = e.Value.Description,
                    duration = e.Value.Duration.TotalMilliseconds,
                    tags = e.Value.Tags,
                    data = e.Value.Data,
                    exception = e.Value.Exception?.Message
                })
        }, new JsonSerializerOptions
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        });
        await context.Response.WriteAsync(result);
    }
});

// --- Prometheus Metrics Endpoint ---
if (metricsEnabled)
{
    app.MapMetrics(); // Expone endpoint /metrics para Prometheus
}

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