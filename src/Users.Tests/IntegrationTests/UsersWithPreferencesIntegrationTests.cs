using Xunit;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using System.Net.Http.Json;
using Users.Application.Dtos;
using Users.Tests.Infrastructure;

namespace Users.Tests.IntegrationTests;

/// <summary>
/// Tests de integración para el controlador UsersWithPreferencesController
/// que maneja la creación y actualización combinada de usuarios y preferencias
/// </summary>
public class UsersWithPreferencesIntegrationTests : IClassFixture<TestWebApplicationFactory<Users.Api.Program>>
{
    private readonly TestWebApplicationFactory<Users.Api.Program> _factory;

    public UsersWithPreferencesIntegrationTests(TestWebApplicationFactory<Users.Api.Program> factory)
    {
        _factory = factory;
    }

    #region Create Tests

    [Fact]
    public async Task CreateUserWithPreferences_ShouldSucceed()
    {
        // Arrange
        var client = _factory.CreateClient();
        var userDto = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Test",
            Lastname: "User",
            Email: $"test_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com",
            Password: "Password123!"
        );

        // Act
        var response = await client.PostAsJsonAsync("/api/users-with-preferences", userDto);

        // Assert
        if (response.StatusCode != HttpStatusCode.Created)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new Exception($"Expected 201, got {response.StatusCode}. Content: {errorContent}");
        }
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("user").GetProperty("nickname").GetString().Should().Be(userDto.Nickname);
        result.GetProperty("user").GetProperty("email").GetString().Should().Be(userDto.Email);
        result.GetProperty("preferences").GetProperty("wcagVersion").GetString().Should().Be("2.2");
        result.GetProperty("preferences").GetProperty("wcagLevel").GetString().Should().Be("AA");
        result.GetProperty("preferences").GetProperty("language").GetString().Should().Be("es");
    }

    [Fact]
    public async Task CreateUserWithPreferences_WithDuplicateEmail_ShouldReturn409()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = $"test_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";

        var userDto1 = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Test",
            Lastname: "User",
            Email: email,
            Password: "Password123!"
        );

        var userDto2 = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Test2",
            Lastname: "User2",
            Email: email, // Mismo email
            Password: "Password123!"
        );

        // Act
        await client.PostAsJsonAsync("/api/users-with-preferences", userDto1);
        var response = await client.PostAsJsonAsync("/api/users-with-preferences", userDto2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateUserWithPreferences_WithDuplicateNickname_ShouldReturn409()
    {
        // Arrange
        var client = _factory.CreateClient();
        var nickname = $"u{Guid.NewGuid().ToString().Substring(0, 6)}";

        var userDto1 = new UserCreateDto(
            Nickname: nickname,
            Name: "Test",
            Lastname: "User",
            Email: $"test1_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com",
            Password: "Password123!"
        );

        var userDto2 = new UserCreateDto(
            Nickname: nickname, // Mismo nickname
            Name: "Test2",
            Lastname: "User2",
            Email: $"test2_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com",
            Password: "Password123!"
        );

        // Act
        await client.PostAsJsonAsync("/api/users-with-preferences", userDto1);
        var response = await client.PostAsJsonAsync("/api/users-with-preferences", userDto2);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    #endregion

    #region Patch Tests

    [Fact]
    public async Task PatchUserAndPreferences_WithoutAuthentication_ShouldReturn401()
    {
        // Arrange
        var client = _factory.CreateClient(); // Cliente sin autenticación
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null, WcagLevel: null, Language: null, VisualTheme: null,
            ReportFormat: null, NotificationsEnabled: null, AiResponseLevel: null, FontSize: null,
            Nickname: null, Name: "Updated Name", Lastname: null, Email: null, Password: null
        );

        // Act
        var response = await client.PatchAsync(
            "/api/users-with-preferences/by-email/any@example.com",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PatchUserAndPreferences_WithNonExistentUser_ShouldReturn404()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null, WcagLevel: null, Language: null, VisualTheme: null,
            ReportFormat: null, NotificationsEnabled: null, AiResponseLevel: null, FontSize: null,
            Nickname: null, Name: "Updated Name", Lastname: null, Email: null, Password: null
        );

        // Act
        var response = await client.PatchAsync(
            "/api/users-with-preferences/by-email/nonexistent@example.com",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PatchUserAndPreferences_ShouldUpdateUserFields()
    {
        // Arrange
        var clientNoAuth = _factory.CreateClient();
        var email = $"patch_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";

        // Crear usuario primero
        var createDto = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Original",
            Lastname: "Name",
            Email: email,
            Password: "Password123!"
        );
        await clientNoAuth.PostAsJsonAsync("/api/users-with-preferences", createDto);

        // Actualizar con cliente autenticado
        var clientAuth = _factory.CreateAuthenticatedClient(email: email);
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null, WcagLevel: null, Language: null, VisualTheme: null,
            ReportFormat: null, NotificationsEnabled: null, AiResponseLevel: null, FontSize: null,
            Nickname: null, Name: "Updated", Lastname: "UpdatedLastname", Email: null, Password: null
        );

        // Act
        var response = await clientAuth.PatchAsync(
            $"/api/users-with-preferences/by-email/{email}",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("user").GetProperty("name").GetString().Should().Be("Updated");
        result.GetProperty("user").GetProperty("lastname").GetString().Should().Be("UpdatedLastname");
    }

    [Fact]
    public async Task PatchUserAndPreferences_ShouldUpdatePreferenceFields()
    {
        // Arrange
        var clientNoAuth = _factory.CreateClient();
        var email = $"pref_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";

        // Crear usuario primero
        var createDto = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Test",
            Lastname: "User",
            Email: email,
            Password: "Password123!"
        );
        await clientNoAuth.PostAsJsonAsync("/api/users-with-preferences", createDto);

        // Actualizar preferencias con cliente autenticado
        var clientAuth = _factory.CreateAuthenticatedClient(email: email);
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: "2.1", WcagLevel: "AAA", Language: "en", VisualTheme: "dark",
            ReportFormat: "html", NotificationsEnabled: false, AiResponseLevel: "detailed", FontSize: 18,
            Nickname: null, Name: null, Lastname: null, Email: null, Password: null
        );

        // Act
        var response = await clientAuth.PatchAsync(
            $"/api/users-with-preferences/by-email/{email}",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("preferences").GetProperty("wcagVersion").GetString().Should().Be("2.1");
        result.GetProperty("preferences").GetProperty("wcagLevel").GetString().Should().Be("AAA");
        result.GetProperty("preferences").GetProperty("language").GetString().Should().Be("en");
        result.GetProperty("preferences").GetProperty("visualTheme").GetString().Should().Be("dark");
        result.GetProperty("preferences").GetProperty("reportFormat").GetString().Should().Be("html");
        result.GetProperty("preferences").GetProperty("notificationsEnabled").GetBoolean().Should().BeFalse();
        result.GetProperty("preferences").GetProperty("aiResponseLevel").GetString().Should().Be("detailed");
        result.GetProperty("preferences").GetProperty("fontSize").GetInt32().Should().Be(18);
    }

    [Fact]
    public async Task PatchUserAndPreferences_WithPartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange
        var clientNoAuth = _factory.CreateClient();
        var email = $"part_{Guid.NewGuid().ToString().Substring(0, 8)}@example.com";

        // Crear usuario primero
        var createDto = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Original",
            Lastname: "LastName",
            Email: email,
            Password: "Password123!"
        );
        await clientNoAuth.PostAsJsonAsync("/api/users-with-preferences", createDto);

        // Actualizar solo el nombre
        var clientAuth = _factory.CreateAuthenticatedClient(email: email);
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null, WcagLevel: null, Language: null, VisualTheme: null,
            ReportFormat: null, NotificationsEnabled: null, AiResponseLevel: null, FontSize: null,
            Nickname: null, Name: "OnlyNameChanged", Lastname: null, Email: null, Password: null
        );

        // Act
        var response = await clientAuth.PatchAsync(
            $"/api/users-with-preferences/by-email/{email}",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        result.GetProperty("user").GetProperty("name").GetString().Should().Be("OnlyNameChanged");
        result.GetProperty("user").GetProperty("lastname").GetString().Should().Be("LastName", "lastname should remain unchanged");
    }

    [Fact]
    public async Task PatchUserAndPreferences_WithInvalidWcagVersion_ShouldNotUpdate()
    {
        // Arrange
        var clientNoAuth = _factory.CreateClient();
        var email = $"inv_{Guid.NewGuid().ToString().Substring(0, 9)}@example.com";

        // Crear usuario primero
        var createDto = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Test",
            Lastname: "User",
            Email: email,
            Password: "Password123!"
        );
        await clientNoAuth.PostAsJsonAsync("/api/users-with-preferences", createDto);

        // Intentar actualizar con versión WCAG inválida
        var clientAuth = _factory.CreateAuthenticatedClient(email: email);
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: "3.0", // Versión no soportada
            WcagLevel: null, Language: null, VisualTheme: null,
            ReportFormat: null, NotificationsEnabled: null, AiResponseLevel: null, FontSize: null,
            Nickname: null, Name: null, Lastname: null, Email: null, Password: null
        );

        // Act
        var response = await clientAuth.PatchAsync(
            $"/api/users-with-preferences/by-email/{email}",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // La versión WCAG debe seguir siendo la por defecto (2.2)
        result.GetProperty("preferences").GetProperty("wcagVersion").GetString().Should().Be("2.2", "invalid WCAG version should be ignored");
    }

    [Fact]
    public async Task PatchUserAndPreferences_WithPassword_ShouldHashPassword()
    {
        // Arrange
        var clientNoAuth = _factory.CreateClient();
        var email = $"pwd_{Guid.NewGuid().ToString().Substring(0, 9)}@example.com";

        // Crear usuario primero
        var createDto = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Test",
            Lastname: "User",
            Email: email,
            Password: "OldPassword123!"
        );
        await clientNoAuth.PostAsJsonAsync("/api/users-with-preferences", createDto);

        // Actualizar contraseña
        var clientAuth = _factory.CreateAuthenticatedClient(email: email);
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null, WcagLevel: null, Language: null, VisualTheme: null,
            ReportFormat: null, NotificationsEnabled: null, AiResponseLevel: null, FontSize: null,
            Nickname: null, Name: null, Lastname: null, Email: null, Password: "NewPassword456!"
        );

        // Act
        var response = await clientAuth.PatchAsync(
            $"/api/users-with-preferences/by-email/{email}",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        // La contraseña no debe aparecer en la respuesta (seguridad)
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotContain("NewPassword456!");
        content.Should().NotContain("password"); // El campo password no debe estar en la respuesta
    }

    [Fact]
    public async Task PatchUserAndPreferences_WithAllFields_ShouldUpdateEverything()
    {
        // Arrange
        var clientNoAuth = _factory.CreateClient();
        var email = $"all_{Guid.NewGuid().ToString().Substring(0, 9)}@example.com";

        // Crear usuario primero
        var createDto = new UserCreateDto(
            Nickname: $"u{Guid.NewGuid().ToString().Substring(0, 6)}",
            Name: "Original",
            Lastname: "Name",
            Email: email,
            Password: "Password123!"
        );
        await clientNoAuth.PostAsJsonAsync("/api/users-with-preferences", createDto);

        // Actualizar TODOS los campos
        var clientAuth = _factory.CreateAuthenticatedClient(email: email);
        var newNickname = $"u{Guid.NewGuid().ToString().Substring(0, 6)}";
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: "2.0", WcagLevel: "A", Language: "en", VisualTheme: "dark",
            ReportFormat: "json", NotificationsEnabled: false, AiResponseLevel: "basic", FontSize: 16,
            Nickname: newNickname, Name: "UpdatedName", Lastname: "UpdatedLastname", Email: email, Password: null
        );

        // Act
        var response = await clientAuth.PatchAsync(
            $"/api/users-with-preferences/by-email/{email}",
            JsonContent.Create(patchDto));

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<JsonElement>(content);

        // Verificar todos los campos del usuario
        result.GetProperty("user").GetProperty("nickname").GetString().Should().Be(newNickname);
        result.GetProperty("user").GetProperty("name").GetString().Should().Be("UpdatedName");
        result.GetProperty("user").GetProperty("lastname").GetString().Should().Be("UpdatedLastname");
        result.GetProperty("user").GetProperty("email").GetString().Should().Be(email);

        // Verificar todos los campos de preferencias
        result.GetProperty("preferences").GetProperty("wcagVersion").GetString().Should().Be("2.0");
        result.GetProperty("preferences").GetProperty("wcagLevel").GetString().Should().Be("A");
        result.GetProperty("preferences").GetProperty("language").GetString().Should().Be("en");
        result.GetProperty("preferences").GetProperty("visualTheme").GetString().Should().Be("dark");
        result.GetProperty("preferences").GetProperty("reportFormat").GetString().Should().Be("json");
        result.GetProperty("preferences").GetProperty("notificationsEnabled").GetBoolean().Should().BeFalse();
        result.GetProperty("preferences").GetProperty("aiResponseLevel").GetString().Should().Be("basic");
        result.GetProperty("preferences").GetProperty("fontSize").GetInt32().Should().Be(16);
    }

    #endregion
}
