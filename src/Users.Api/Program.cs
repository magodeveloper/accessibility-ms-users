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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Registrar controladores MVC
builder.Services.AddControllers();

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<IPasswordService, BcryptPasswordService>();
builder.Services.AddSingleton<ISessionTokenService, SessionTokenService>();

// Servicios de dominio
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IPreferenceService, PreferenceService>();
builder.Services.AddScoped<ISessionService, SessionService>();

// FluentValidation: registra todos los validadores del ensamblado
builder.Services.AddValidatorsFromAssemblyContaining<UserCreateDtoValidator>();

var app = builder.Build();

// Migración automática de la base de datos al iniciar la API
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    await db.Database.MigrateAsync();
}

// Middleware global para manejo de errores uniformes
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json";
        // Detectar idioma desde el header Accept-Language o default 'es'
        var lang = context.Request.Headers["Accept-Language"].FirstOrDefault()?.Split(',')[0] ?? "es";
        string Get(string key, string lang) => Users.Api.Localization.Get(key, lang);
        var result = JsonSerializer.Serialize(new { error = Get("Error_InternalServer", lang) });
        context.Response.StatusCode = 500;
        await context.Response.WriteAsync(result);
    });
});

// Habilitar el enrutamiento de controladores
app.MapControllers();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

await app.RunAsync();

// Necesario para tests de integración (WebApplicationFactory)
namespace Users.Api
{
    public partial class Program { protected Program() { } }
}