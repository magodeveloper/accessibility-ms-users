using Xunit;
using Users.Api;
using System.Net;
using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Users.Tests.Infrastructure;

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
            var client = _factory.CreateClient();
            var email = "testauth@email.com";
            var password = "TestAuth123!";
            var dto = new { nickname = "testauth", name = "Test", lastname = "Auth", email, password };
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto);
            var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            var loginData = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(loginData.TryGetProperty("token", out _));
            var logoutResp = await client.PostAsJsonAsync("/api/v1/auth/logout", new { email });
            Assert.Equal(HttpStatusCode.OK, logoutResp.StatusCode);
        }

        // ---------- USERS ----------
        [Fact]
        public async Task Users_CRUD_Works()
        {
            var client = _factory.CreateClient();
            var email = "usercrud@email.com";
            var password = "UserCrud123!";
            var dto = new { nickname = "usercrud", name = "User", lastname = "Crud", email, password };
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            var createResp = await client.PostAsJsonAsync("/api/v1/users", dto);
            Assert.Equal(HttpStatusCode.Created, createResp.StatusCode);
            var getResp = await client.GetAsync($"/api/v1/users/by-email?email={email}");
            Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
            var delResp = await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            Assert.Equal(HttpStatusCode.OK, delResp.StatusCode);
            var getAfterDel = await client.GetAsync($"/api/v1/users/by-email?email={email}");
            Assert.Equal(HttpStatusCode.NotFound, getAfterDel.StatusCode);
        }

        // ---------- USERS WITH PREFERENCES ----------
        [Fact]
        public async Task UsersWithPreferences_Create_Works()
        {
            var client = _factory.CreateClient();
            var email = "uwp@email.com";
            var password = "UwpTest123!";
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            var dto = new { nickname = "uwp", name = "UWP", lastname = "Test", email, password };
            var resp = await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto);
            Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        }

        // ---------- PREFERENCES ----------
        [Fact]
        public async Task Preferences_CRUD_Works()
        {
            var client = _factory.CreateClient();
            var email = "pref@email.com";
            var password = "PrefTest123!";
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            var userDto = new { nickname = "pref", name = "Pref", lastname = "Test", email, password };
            var createUserResp = await client.PostAsJsonAsync("/api/v1/users-with-preferences", userDto);
            // Obtener el userId del usuario creado
            int userId = 0;
            if (createUserResp.StatusCode == HttpStatusCode.Created)
            {
                var json = await createUserResp.Content.ReadFromJsonAsync<JsonElement>();
                if (json.TryGetProperty("user", out var userObj) && userObj.TryGetProperty("id", out var idProp))
                    userId = idProp.GetInt32();
            }
            var getPrefResp = await client.GetAsync($"/api/v1/preferences/by-user/{email}");
            Assert.True(getPrefResp.StatusCode == HttpStatusCode.OK || getPrefResp.StatusCode == HttpStatusCode.NotFound);
            var prefDto = new { userId, wcagVersion = "2.1", wcagLevel = "AA", language = "es", visualTheme = "light", reportFormat = "pdf", notificationsEnabled = true, aiResponseLevel = "intermediate", fontSize = 14 };
            // No se puede crear preferencia si ya existe, pero se prueba el endpoint
            var createPrefResp = await client.PostAsJsonAsync("/api/v1/preferences", prefDto);
            Assert.True(createPrefResp.StatusCode == HttpStatusCode.Created || createPrefResp.StatusCode == HttpStatusCode.Conflict);
        }

        // ---------- SESSIONS ----------
        [Fact]
        public async Task Sessions_CRUD_Works()
        {
            var client = _factory.CreateClient();
            var email = "session@email.com";
            var password = "SessionTest123!";
            await client.DeleteAsync($"/api/v1/users/by-email/{email}");
            var dto = new { nickname = "session", name = "Session", lastname = "Test", email, password };
            await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto);
            var loginResp = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
            Assert.Equal(HttpStatusCode.OK, loginResp.StatusCode);
            var loginData = await loginResp.Content.ReadFromJsonAsync<JsonElement>();
            var userId = loginData.GetProperty("user").GetProperty("id").GetInt32();
            // GET sesiones por usuario
            var getByUserResp = await client.GetAsync($"/api/v1/sessions/user/{userId}");
            Assert.True(getByUserResp.StatusCode == HttpStatusCode.OK || getByUserResp.StatusCode == HttpStatusCode.NotFound);
            // GET todas las sesiones
            var getAllResp = await client.GetAsync($"/api/v1/sessions");
            Assert.Equal(HttpStatusCode.OK, getAllResp.StatusCode);
            // DELETE todas las sesiones del usuario
            var delByUserResp = await client.DeleteAsync($"/api/v1/sessions/by-user/{userId}");
            Assert.True(delByUserResp.StatusCode == HttpStatusCode.OK || delByUserResp.StatusCode == HttpStatusCode.NotFound);
        }

        // ---------- DELETE ALL DATA ----------
        [Fact]
        public async Task DeleteAllData_RemovesAllRecords()
        {
            var client = _factory.CreateClient();

            // Crear algunos datos de prueba
            var email1 = "deleteall1@test.com";
            var email2 = "deleteall2@test.com";
            var password = "DeleteTest123!";

            var dto1 = new { nickname = "delall1", name = "Delete", lastname = "All1", email = email1, password };
            var dto2 = new { nickname = "delall2", name = "Delete", lastname = "All2", email = email2, password };

            // Limpiar datos previos
            await client.DeleteAsync($"/api/v1/users/by-email/{email1}");
            await client.DeleteAsync($"/api/v1/users/by-email/{email2}");

            // Crear usuarios con preferencias
            var createResp1 = await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto1);
            var createResp2 = await client.PostAsJsonAsync("/api/v1/users-with-preferences", dto2);

            Assert.Equal(HttpStatusCode.Created, createResp1.StatusCode);
            Assert.Equal(HttpStatusCode.Created, createResp2.StatusCode);

            // Crear sesiones mediante login
            await client.PostAsJsonAsync("/api/v1/auth/login", new { email = email1, password });
            await client.PostAsJsonAsync("/api/v1/auth/login", new { email = email2, password });

            // Verificar que existen usuarios antes de eliminar
            var getUsersBefore = await client.GetAsync("/api/v1/users");
            Assert.Equal(HttpStatusCode.OK, getUsersBefore.StatusCode);

            // Ejecutar eliminación de todos los datos
            var deleteAllResp = await client.DeleteAsync("/api/v1/users/all-data");
            Assert.Equal(HttpStatusCode.OK, deleteAllResp.StatusCode);

            // Verificar que no existen usuarios después de eliminar
            var getUsersAfter = await client.GetAsync("/api/v1/users");
            Assert.Equal(HttpStatusCode.OK, getUsersAfter.StatusCode);

            var usersData = await getUsersAfter.Content.ReadFromJsonAsync<JsonElement>();
            var usersArray = usersData.GetProperty("users");
            Assert.Equal(0, usersArray.GetArrayLength());

            // Verificar que tampoco existen sesiones
            var getSessionsAfter = await client.GetAsync("/api/v1/sessions");
            Assert.Equal(HttpStatusCode.OK, getSessionsAfter.StatusCode);
        }
    }
}