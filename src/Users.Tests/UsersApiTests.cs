using Xunit;
using Users.Api;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.Tests
{
    public class UsersApiTests : IClassFixture<WebApplicationFactory<Users.Api.Program>>
    {
        private readonly WebApplicationFactory<Users.Api.Program> _factory;

        public UsersApiTests(WebApplicationFactory<Users.Api.Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Logout_EliminaSesionesYActualizaLastLogin()
        {
            var client = _factory.CreateClient();
            var email = "logout@email.com";
            var password = "LogoutTest123!";
            var dto = new { nickname = "logoutuser", name = "Logout", lastname = "User", email, password };
            // Eliminar usuario si existe
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            // Crear usuario
            var createResp = await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

            // Login para crear sesión
            var loginDto = new { email, password };
            var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            var loginData = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(loginData.TryGetProperty("token", out var tokenProp));
            Assert.False(string.IsNullOrWhiteSpace(tokenProp.GetString()));

            // Logout
            var logoutDto = new { email };
            var logoutResp = await client.PostAsJsonAsync("/api/v1/auth/logout", logoutDto);
            Assert.Equal(HttpStatusCode.OK, logoutResp.StatusCode);

            // Intentar login nuevamente (debe permitir, pero last_login debe ser null antes del login)
            // Consultar usuario para verificar last_login null

            var getUserResp = await client.GetAsync($"/api/v1/users/by-email?email={email}");
            if (getUserResp.StatusCode == HttpStatusCode.OK)
            {
                var userData = await getUserResp.Content.ReadFromJsonAsync<JsonElement>();
                if (userData.TryGetProperty("user", out var user) && user.TryGetProperty("lastLogin", out var lastLoginProp))
                {
                    Assert.True(lastLoginProp.ValueKind == JsonValueKind.Null || string.IsNullOrWhiteSpace(lastLoginProp.GetString()));
                }
            }

            // Login nuevamente para restaurar last_login
            var reloginResp = await client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            Assert.Equal(HttpStatusCode.OK, reloginResp.StatusCode);
            var reloginData = await reloginResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(reloginData.TryGetProperty("token", out var reloginTokenProp));
            Assert.False(string.IsNullOrWhiteSpace(reloginTokenProp.GetString()));
        }

        [Fact]
        public async Task LoginUsuarioRecienCreado_DevuelveTokenCorrecto()
        {
            var client = _factory.CreateClient();
            var email = "pruebas@email.com";
            var password = "TestLogin123!";
            var dto = new { nickname = "pruebas", name = "Login", lastname = "User", email, password };
            // Eliminar usuario si existe
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            // Crear usuario
            var createResp = await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

            // Login
            var loginDto = new { email, password };
            var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", loginDto);
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            var data = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(data.TryGetProperty("token", out var tokenProp));
            Assert.False(string.IsNullOrWhiteSpace(tokenProp.GetString()));
            Assert.True(data.TryGetProperty("expiresAt", out _));
        }

        [Fact]
        public async Task ObtenerPreferenciasPorEmail_DevuelvePreferenciasCorrectas()
        {
            var client = _factory.CreateClient();
            var dto = new { nickname = "mailuser", name = "Mail", lastname = "User", email = "mail@email.com", password = "Test1234!" };
            await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto);
            var resp = await client.GetAsync("/api/v1/preferences/by-user/mail@email.com");
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            var data = await resp.Content.ReadFromJsonAsync<JsonElement>();
            var prefs = data.GetProperty("preferences");
            Assert.Equal("mail@email.com", prefs.GetProperty("user").GetProperty("email").GetString());
        }

        [Fact]
        public async Task PatchUsuarioYPreferenciasPorEmail_ActualizaCorrectamente()
        {
            var client = _factory.CreateClient();
            var email = "patch@email.com";
            // Eliminar usuario si existe
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            var dto = new { nickname = "patchuser", name = "Patch", lastname = "User", email, password = "Test1234!" };
            await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto);
            // Eliminar usuario destino si existe
            await client.DeleteAsync($"/api/v1/users/by-email/patch2@email.com");
            var patch = new
            {
                wcagVersion = "2.2",
                wcagLevel = "AAA",
                language = "en",
                visualTheme = "dark",
                reportFormat = "html",
                notificationsEnabled = false,
                aiResponseLevel = "detailed",
                fontSize = 20,
                nickname = "patchuser2",
                name = "Patched",
                lastname = "User2",
                role = "admin",
                status = "inactive",
                emailConfirmed = true,
                email = "patch2@email.com",
                password = "NewPass123!"
            };
            var resp = await client.PatchAsJsonAsync("/api/v1/preferences/by-user/patch@email.com", patch);
            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
            // Verificar cambios
            var getPrefs = await client.GetAsync("/api/v1/preferences/by-user/patch2@email.com");
            Assert.Equal(HttpStatusCode.OK, getPrefs.StatusCode);
            var data = await getPrefs.Content.ReadFromJsonAsync<JsonElement>();
            var prefs = data.GetProperty("preferences");
            Assert.Equal("2.2", prefs.GetProperty("wcagVersion").GetString());
            Assert.Equal("AAA", prefs.GetProperty("wcagLevel").GetString());
            Assert.Equal("en", prefs.GetProperty("language").GetString());
            Assert.Equal("dark", prefs.GetProperty("visualTheme").GetString());
            Assert.Equal("html", prefs.GetProperty("reportFormat").GetString());
            Assert.Equal("detailed", prefs.GetProperty("aiResponseLevel").GetString());
            Assert.Equal(20, prefs.GetProperty("fontSize").GetInt32());
            Assert.False(prefs.GetProperty("notificationsEnabled").GetBoolean());
            Assert.Equal("patch2@email.com", prefs.GetProperty("user").GetProperty("email").GetString());
            Assert.Equal("Patched", prefs.GetProperty("user").GetProperty("name").GetString());
            Assert.Equal("User2", prefs.GetProperty("user").GetProperty("lastname").GetString());
            Assert.Equal("admin", prefs.GetProperty("user").GetProperty("role").GetString());
            Assert.Equal("inactive", prefs.GetProperty("user").GetProperty("status").GetString());
            Assert.True(prefs.GetProperty("user").GetProperty("emailConfirmed").GetBoolean());
        }

        [Fact]
        public async Task DeleteUsuarioPorEmail_EliminaCorrectamente()
        {
            var client = _factory.CreateClient();
            var dto = new { nickname = "deluser", name = "Del", lastname = "User", email = "del@email.com", password = "Test1234!" };
            await client.PostAsJsonAsync("/api/v1/users", dto);
            var delResp = await client.DeleteAsync("/api/v1/users/by-email/del@email.com");
            Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
            var getResp = await client.GetAsync("/api/v1/users/by-email?email=del@email.com");
            Assert.Equal(HttpStatusCode.NotFound, getResp.StatusCode);
        }
    }
}