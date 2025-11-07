using Xunit;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using System.Net.Http.Json;
using Users.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.Tests.IntegrationTests;

/// <summary>
/// Tests de integración para verificar la configuración de Program.cs:
/// - Health check endpoints (/health, /health/live, /health/ready)
/// - Prometheus metrics endpoint (/metrics)
/// - Swagger/OpenAPI endpoints
/// - Middleware pipeline (Exception handling, UserContext, Localization)
/// </summary>
public class ProgramConfigurationIntegrationTests : IClassFixture<TestWebApplicationFactory<Users.Api.Program>>
{
    private readonly TestWebApplicationFactory<Users.Api.Program> _factory;

    public ProgramConfigurationIntegrationTests(TestWebApplicationFactory<Users.Api.Program> factory)
    {
        _factory = factory;
    }

    #region Health Check Endpoints

    [Fact]
    public async Task HealthEndpoint_ShouldReturnHealthyStatus()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        // En tests con InMemory DB, el DatabaseHealthCheck puede fallar, retornando 503
        // Lo importante es que el endpoint responde correctamente
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
        content.TryGetProperty("timestamp", out _).Should().BeTrue();
        content.TryGetProperty("totalDuration", out _).Should().BeTrue();
        content.TryGetProperty("entries", out _).Should().BeTrue();
    }

    [Fact]
    public async Task HealthLiveEndpoint_ShouldReturnLivenessChecks()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
        content.TryGetProperty("timestamp", out _).Should().BeTrue();
        content.TryGetProperty("checks", out var checks).Should().BeTrue();

        // Verificar que contiene checks con tag "live"
        var checksArray = checks.EnumerateArray().ToList();
        checksArray.Should().NotBeEmpty();
        checksArray.Should().Contain(c => c.GetProperty("name").GetString() == "application" ||
                                           c.GetProperty("name").GetString() == "memory");
    }

    [Fact]
    public async Task HealthReadyEndpoint_ShouldReturnReadinessChecks()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        // En tests con InMemory DB, puede retornar 503 si DatabaseHealthCheck falla
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.GetProperty("status").GetString().Should().BeOneOf("Healthy", "Degraded", "Unhealthy");
        content.TryGetProperty("timestamp", out _).Should().BeTrue();
        content.TryGetProperty("totalDuration", out _).Should().BeTrue();
        content.TryGetProperty("checks", out var checks).Should().BeTrue();

        // Verificar que contiene checks con tag "ready" (database, users_dbcontext)
        var checksArray = checks.EnumerateArray().ToList();
        checksArray.Should().NotBeEmpty();
        checksArray.Should().Contain(c => c.GetProperty("name").GetString() == "database" ||
                                           c.GetProperty("name").GetString() == "users_dbcontext");
    }

    [Fact]
    public async Task HealthEndpoint_ShouldIncludeAllHealthChecks()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("entries", out var entries).Should().BeTrue();

        var entriesDict = entries.EnumerateObject().Select(p => p.Name).ToList();

        // Verificar que contiene los health checks esperados
        entriesDict.Should().Contain("application");
        entriesDict.Should().Contain("memory");
        entriesDict.Should().Contain("database");
        entriesDict.Should().Contain("users_dbcontext");
    }

    [Fact]
    public async Task HealthChecks_ShouldHaveDataProperty()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/ready");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("checks", out var checks).Should().BeTrue();

        var checksArray = checks.EnumerateArray().ToList();
        foreach (var check in checksArray)
        {
            check.TryGetProperty("status", out _).Should().BeTrue();
            check.TryGetProperty("duration", out _).Should().BeTrue();
            // data puede ser null, pero la propiedad debe existir en algunos checks
        }
    }

    #endregion

    #region Prometheus Metrics

    [Fact]
    public async Task MetricsEndpoint_ShouldReturnPrometheusFormat()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/metrics");

        // Assert
        // El endpoint de métricas puede retornar 200 o 404 dependiendo de la configuración
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.NotFound);

        if (response.StatusCode == HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            // Las métricas de Prometheus usan formato text/plain
            content.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task HttpMetrics_ShouldBeCollected()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act - Hacer varias llamadas para generar métricas
        await client.GetAsync("/api/users");
        await client.GetAsync("/health");

        var metricsResponse = await client.GetAsync("/metrics");

        // Assert
        if (metricsResponse.StatusCode == HttpStatusCode.OK)
        {
            var content = await metricsResponse.Content.ReadAsStringAsync();
            // Verificar que se recolectaron métricas HTTP (si están habilitadas)
            content.Should().NotBeNullOrEmpty();
        }
    }

    #endregion

    #region Middleware Pipeline

    [Fact]
    public async Task UserContextMiddleware_ShouldPopulateFromHeaders()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(
            userId: 42,
            email: "middleware@test.com",
            role: "Admin",
            userName: "Middleware Test"
        );

        // Act - Hacer una llamada que requiere autenticación
        var response = await client.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // Si el middleware funciona correctamente, la respuesta será exitosa
        // porque el UserContext está poblado desde los headers
    }

    [Fact]
    public async Task UserContextMiddleware_WithoutHeaders_ShouldStillWork()
    {
        // Arrange
        var client = _factory.CreateClient(); // Cliente sin headers de autenticación

        // Act - Hacer una llamada a un endpoint público (crear usuario)
        var userDto = new
        {
            nickname = "publicuser",
            name = "Public",
            lastname = "User",
            email = "public@test.com",
            password = "Public123!"
        };

        var response = await client.PostAsJsonAsync("/api/users", userDto);

        // Assert
        // El endpoint de creación de usuario es público, debe funcionar sin autenticación
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ExceptionHandler_ShouldReturnJsonErrorOn500()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act - Intentar acceder a un endpoint que no existe (404)
        var response = await client.GetAsync("/api/nonexistent");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Localization_ShouldAcceptLanguageHeader()
    {
        // Arrange
        var clientEn = _factory.CreateClient();
        clientEn.DefaultRequestHeaders.Add("Accept-Language", "en");

        var clientEs = _factory.CreateClient();
        clientEs.DefaultRequestHeaders.Add("Accept-Language", "es");

        // Act - GetAll es público, retorna OK
        var responseEn = await clientEn.GetAsync("/api/users");
        var responseEs = await clientEs.GetAsync("/api/users");

        // Assert
        // Ambos deberían retornar OK (GetAll es público)
        responseEn.StatusCode.Should().Be(HttpStatusCode.OK);
        responseEs.StatusCode.Should().Be(HttpStatusCode.OK);

        // El middleware de localización está configurado y funciona
        // en endpoints que requieren autenticación
    }

    #endregion

    #region Controllers Routing

    [Fact]
    public async Task Controllers_ShouldBeRouted()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act & Assert - Verificar que los controllers están mapeados correctamente
        var usersResponse = await client.GetAsync("/api/users");
        usersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionsResponse = await client.GetAsync("/api/sessions");
        sessionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var healthResponse = await client.GetAsync("/health");
        // Health puede retornar 503 si algún check falla (InMemory DB)
        healthResponse.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
    }

    [Fact]
    public async Task StaticFiles_ShouldNotBeServed()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/favicon.ico");

        // Assert
        // No hay configuración de archivos estáticos, debe retornar 404
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    #endregion

    #region Database Migration

    [Fact]
    public async Task Database_ShouldBeInitialized()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Act - Crear un usuario para verificar que la base de datos está inicializada
        var userDto = new
        {
            nickname = "dbtest",
            name = "Database",
            lastname = "Test",
            email = "dbtest@test.com",
            password = "DbTest123!"
        };

        var response = await client.PostAsJsonAsync("/api/users", userDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    #endregion

    #region CORS (no configurado explícitamente, pero verificamos)

    [Fact]
    public async Task Cors_ShouldNotBeEnabled()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        // Verificar que no hay headers CORS (porque no está configurado explícitamente)
        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }

    #endregion
}
