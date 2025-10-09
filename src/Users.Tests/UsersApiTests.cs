using Xunit;
using Users.Api;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Users.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.Tests
{
    public class UsersApiTests : IClassFixture<TestWebApplicationFactory<Users.Api.Program>>
    {
        private readonly TestWebApplicationFactory<Users.Api.Program> _factory;

        public UsersApiTests(TestWebApplicationFactory<Users.Api.Program> factory)
        {
            _factory = factory;
        }

        // ---------- AUTH ----------
        [Fact]
        public async Task Auth_Login_And_Logout_Works()
        {
            var client = _factory.CreateAuthenticatedClient(); // Usuario autenticado para crear/eliminar usuarios
            var email = "testauth@email.com";
            var password = "TestAuth123!";
            var dto = new { nickname = "testauth", name = "Test", lastname = "Auth", email, password };
            await client.DeleteAsync($"/api/users/by-email/{email}");
            await client.PostAsJsonAsync("/api/users-with-preferences", dto);

            // Login NO requiere autenticación (endpoint público)
            var unauthClient = _factory.CreateClient();
            var loginResp = await unauthClient.PostAsJsonAsync("/api/auth/login", new { email, password });
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            var loginData = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(loginData.TryGetProperty("token", out _));

            // Logout usa cliente autenticado
            var logoutResp = await client.PostAsJsonAsync("/api/auth/logout", new { email });
            Assert.Equal(HttpStatusCode.OK, logoutResp.StatusCode);
        }

        // ---------- USERS ----------
        [Fact]
        public async Task Users_CRUD_Works()
        {
            var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado
            var email = "usercrud@email.com";
            var password = "UserCrud123!";
            var dto = new { nickname = "usercrud", name = "User", lastname = "Crud", email, password };

            // Clean up any existing user first
            await client.DeleteAsync($"/api/users/by-email/{email}");

            // Create user
            var createResp = await client.PostAsJsonAsync("/api/users", dto);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);

            // Add small delay to ensure consistency
            await Task.Delay(100);

            // Verify user exists
            var getResp = await client.GetAsync($"/api/users/by-email?email={email}");
            if (getResp.StatusCode != HttpStatusCode.OK)
            {
                var content = await getResp.Content.ReadAsStringAsync();
                Assert.Fail($"Expected OK but got {getResp.StatusCode}. Response: {content}");
            }
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);

            // Delete user
            var delResp = await client.DeleteAsync($"/api/users/by-email/{email}");
            Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);

            // Verify user no longer exists
            var getAfterDel = await client.GetAsync($"/api/users/by-email?email={email}");
            Assert.Equal(HttpStatusCode.NotFound, getAfterDel.StatusCode);
        }

        // ---------- USERS WITH PREFERENCES ----------
        [Fact]
        public async Task UsersWithPreferences_Create_Works()
        {
            var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado
            var email = "uwp@email.com";
            var password = "UwpTest123!";
            await client.DeleteAsync($"/api/users/by-email/{email}");
            var dto = new { nickname = "uwp", name = "UWP", lastname = "Test", email, password };
            var resp = await client.PostAsJsonAsync("/api/users-with-preferences", dto);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        // ---------- PREFERENCES ----------
        [Fact]
        public async Task Preferences_CRUD_Works()
        {
            var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado
            var email = "pref@email.com";
            var password = "PrefTest123!";
            await client.DeleteAsync($"/api/users/by-email/{email}");
            var userDto = new { nickname = "pref", name = "Pref", lastname = "Test", email, password };
            var createUserResp = await client.PostAsJsonAsync("/api/users-with-preferences", userDto);
            // Obtener el userId del usuario creado
            int userId = 0;
            if (createUserResp.StatusCode == HttpStatusCode.Created)
            {
                var json = await createUserResp.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("user", out var userObj) && userObj.TryGetProperty("id", out var idProp))
                    userId = idProp.GetInt32();
            }
            var getPrefResp = await client.GetAsync($"/api/preferences/by-user/{email}");
            Assert.True(getPrefResp.StatusCode == HttpStatusCode.OK || getPrefResp.StatusCode == HttpStatusCode.NotFound);
            var prefDto = new { userId, wcagVersion = "2.1", wcagLevel = "AA", language = "es", visualTheme = "light", reportFormat = "pdf", notificationsEnabled = true, aiResponseLevel = "intermediate", fontSize = 14 };
            // No se puede crear preferencia si ya existe, pero se prueba el endpoint
            var createPrefResp = await client.PostAsJsonAsync("/api/preferences", prefDto);
            Assert.True(createPrefResp.StatusCode == HttpStatusCode.Created || createPrefResp.StatusCode == HttpStatusCode.Conflict);
        }

        // ---------- SESSIONS ----------
        [Fact]
        public async Task Sessions_CRUD_Works()
        {
            var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado
            var email = "session@email.com";
            var password = "SessionTest123!";
            await client.DeleteAsync($"/api/users/by-email/{email}");
            var dto = new { nickname = "session", name = "Session", lastname = "Test", email, password };
            await client.PostAsJsonAsync("/api/users-with-preferences", dto);
            var loginResp = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            var loginData = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            var userId = loginData.GetProperty("user").GetProperty("id").GetInt32();
            // GET sesiones por usuario
            var getByUserResp = await client.GetAsync($"/api/sessions/user/{userId}");
            Assert.True(getByUserResp.StatusCode == HttpStatusCode.OK || getByUserResp.StatusCode == HttpStatusCode.NotFound);
            // GET todas las sesiones
            var getAllResp = await client.GetAsync($"/api/sessions");
            Assert.Equal(HttpStatusCode.OK, getAllResp.StatusCode);
            // DELETE todas las sesiones del usuario
            var delByUserResp = await client.DeleteAsync($"/api/sessions/by-user/{userId}");
            Assert.True(delByUserResp.StatusCode == HttpStatusCode.OK || delByUserResp.StatusCode == HttpStatusCode.NotFound);
        }

        // ---------- DELETE ALL DATA ----------
        [Fact]
        public async Task DeleteAllData_RemovesAllRecords()
        {
            var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado

            // Crear algunos datos de prueba con emails únicos
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var email1 = $"del1_{timestamp}@test.com";
            var email2 = $"del2_{timestamp}@test.com";
            var password = "Test1234"; // Mínimo 8 caracteres según validación FluentValidation

            var dto1 = new { nickname = $"del1_{timestamp}", name = "Delete", lastname = "All1", email = email1, password };
            var dto2 = new { nickname = $"del2_{timestamp}", name = "Delete", lastname = "All2", email = email2, password };

            // Limpiar datos previos
            await client.DeleteAsync($"/api/users/by-email/{email1}");
            await client.DeleteAsync($"/api/users/by-email/{email2}");

            // Crear usuarios con preferencias
            var createResp1 = await client.PostAsJsonAsync("/api/users-with-preferences", dto1);
            var createResp2 = await client.PostAsJsonAsync("/api/users-with-preferences", dto2);

            Assert.Equal(HttpStatusCode.Created, createResp1.StatusCode);
            Assert.Equal(HttpStatusCode.Created, createResp2.StatusCode);

            // Crear sesiones mediante login
            await client.PostAsJsonAsync("/api/auth/login", new { email = email1, password });
            await client.PostAsJsonAsync("/api/auth/login", new { email = email2, password });

            // Verificar que existen usuarios antes de eliminar
            var getUsersBefore = await client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.OK, getUsersBefore.StatusCode);

            // Ejecutar eliminación de todos los datos
            var deleteAllResp = await client.DeleteAsync("/api/users/all-data");
            Assert.Equal(HttpStatusCode.OK, deleteAllResp.StatusCode);

            // Verificar que no existen usuarios después de eliminar
            var getUsersAfter = await client.GetAsync("/api/users");
            Assert.Equal(HttpStatusCode.OK, getUsersAfter.StatusCode);

            var usersData = await getUsersAfter.Content.ReadFromJsonAsync<JsonElement>();
            var usersArray = usersData.GetProperty("users");
            Assert.Equal(0, usersArray.GetArrayLength());

            // Verificar que tampoco existen sesiones
            var getSessionsAfter = await client.GetAsync("/api/sessions");
            Assert.Equal(HttpStatusCode.OK, getSessionsAfter.StatusCode);
        }
    }
}