using Xunit;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using Users.Tests.Helpers;
using System.Net.Http.Json;
using Users.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.Tests.IntegrationTests;

public class AuthIntegrationTests : IClassFixture<TestWebApplicationFactory<Users.Api.Program>>
{
    private readonly TestWebApplicationFactory<Users.Api.Program> _factory;

    public AuthIntegrationTests(TestWebApplicationFactory<Users.Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AuthenticationFlow_ShouldWorkEndToEnd()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado para crear/eliminar usuarios
        var unauthClient = _factory.CreateClient(); // Cliente sin autenticación para login
        var email = "authflow@test.com";
        var password = "AuthFlow123!";
        var userDto = new { nickname = "authflow", name = "Auth", lastname = "Flow", email, password };

        // Clean up any existing data
        await client.DeleteAsync($"/api/users/by-email/{email}");

        // Act & Assert

        // 1. Create user with preferences
        var createUserResponse = await client.PostAsJsonAsync("/api/users-with-preferences", userDto);
        createUserResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createUserContent = await createUserResponse.Content.ReadFromJsonAsync<JsonElement>();
        createUserContent.TryGetProperty("user", out var userElement).Should().BeTrue();
        userElement.TryGetProperty("id", out var userIdElement).Should().BeTrue();
        var userId = userIdElement.GetInt32();

        // 2. Login successfully (login es endpoint público - sin autenticación)
        var loginResponse = await unauthClient.PostAsJsonAsync("/api/auth/login", new { email, password });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        loginContent.TryGetProperty("token", out var tokenElement).Should().BeTrue();
        loginContent.TryGetProperty("user", out _).Should().BeTrue();

        var token = tokenElement.GetString();
        token.Should().NotBeNullOrEmpty();

        // 3. Verify session was created (requiere autenticación)
        var sessionsResponse = await client.GetAsync($"/api/sessions/user/{userId}");
        sessionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionsContent = await sessionsResponse.Content.ReadFromJsonAsync<JsonElement>();
        sessionsContent.TryGetProperty("sessions", out var sessionsArray).Should().BeTrue();
        sessionsArray.GetArrayLength().Should().BeGreaterThan(0);

        // 4. Logout successfully (logout es endpoint público)
        var logoutResponse = await unauthClient.PostAsJsonAsync("/api/auth/logout", new { email });
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verify session was deleted (requiere autenticación)
        var sessionsAfterLogoutResponse = await client.GetAsync($"/api/sessions/user/{userId}");
        if (sessionsAfterLogoutResponse.StatusCode == HttpStatusCode.OK)
        {
            var sessionsAfterLogoutContent = await sessionsAfterLogoutResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (sessionsAfterLogoutContent.TryGetProperty("sessions", out var sessionsAfterLogoutArray))
            {
                sessionsAfterLogoutArray.GetArrayLength().Should().Be(0);
            }
        }

        // Cleanup
        await client.DeleteAsync($"/api/users/by-email/{email}");
    }

    [Fact]
    public async Task Login_ShouldFail_WithInvalidCredentials()
    {
        // Arrange
        var client = _factory.CreateClient();
        var email = "invalid@test.com";
        var password = "wrongpassword";

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var content = await response.Content.ReadFromJsonAsync<JsonElement>();
        content.TryGetProperty("error", out var errorElement).Should().BeTrue();
        errorElement.GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_ShouldUpdateLastLogin()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado para crear/eliminar usuarios
        var unauthClient = _factory.CreateClient(); // Cliente sin auth para login
        var email = "lastlogin@test.com";
        var password = "LastLogin123!";
        var userDto = new { nickname = "lastlogin", name = "Last", lastname = "Login", email, password };

        await client.DeleteAsync($"/api/users/by-email/{email}");
        await client.PostAsJsonAsync("/api/users-with-preferences", userDto);

        // Act - login es público
        var loginResponse = await unauthClient.PostAsJsonAsync("/api/auth/login", new { email, password });

        // Assert
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Get user requiere autenticación
        var getUserResponse = await client.GetAsync($"/api/users/by-email?email={email}");
        getUserResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var userContent = await getUserResponse.Content.ReadFromJsonAsync<JsonElement>();
        userContent.TryGetProperty("user", out var userElement).Should().BeTrue();
        userElement.TryGetProperty("lastLogin", out var lastLoginElement).Should().BeTrue();

        var lastLogin = lastLoginElement.GetDateTime();
        // Comparar con hora de Ecuador (UTC-5)
        lastLogin.Should().BeCloseTo(DateTimeHelper.EcuadorNow, TimeSpan.FromMinutes(1));

        // Cleanup
        await client.DeleteAsync($"/api/users/by-email/{email}");
    }

    [Theory]
    [InlineData("", "password123")]
    [InlineData("test@example.com", "")]
    [InlineData("invalid-email", "password123")]
    public async Task Login_ShouldReturnBadRequest_WithInvalidInput(string email, string password)
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_ShouldWork_EvenWithoutActiveSession()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado para crear/eliminar usuarios
        var unauthClient = _factory.CreateClient(); // Cliente sin auth para logout (es público)
        var email = "nologout@test.com";
        var password = "NoLogout123!";
        var userDto = new { nickname = "nologout", name = "No", lastname = "Logout", email, password };

        await client.DeleteAsync($"/api/users/by-email/{email}");
        await client.PostAsJsonAsync("/api/users-with-preferences", userDto);

        // Act - logout without login (logout es público)
        var logoutResponse = await unauthClient.PostAsJsonAsync("/api/auth/logout", new { email });

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Cleanup
        await client.DeleteAsync($"/api/users/by-email/{email}");
    }
}