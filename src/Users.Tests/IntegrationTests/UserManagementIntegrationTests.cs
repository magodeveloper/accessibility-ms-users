using Xunit;
using System.Net;
using System.Text.Json;
using FluentAssertions;
using System.Net.Http.Json;
using Users.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.Tests.IntegrationTests;

public class UserManagementIntegrationTests : IClassFixture<TestWebApplicationFactory<Users.Api.Program>>
{
    private readonly TestWebApplicationFactory<Users.Api.Program> _factory;

    public UserManagementIntegrationTests(TestWebApplicationFactory<Users.Api.Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UserCRUD_ShouldWorkCompletely()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient(); // Cliente autenticado para todas las operaciones
        var email = "usercrud@integration.test";
        var password = "UserCrud123!";
        var userDto = new
        {
            nickname = "usercrud",
            name = "User",
            lastname = "CRUD",
            email,
            password
        };

        // Clean up
        await client.DeleteAsync($"/api/users/by-email/{email}");

        // Act & Assert

        // 1. Create user
        var createResponse = await client.PostAsJsonAsync("/api/users", userDto);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var createContent = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        createContent.TryGetProperty("id", out var userIdElement).Should().BeTrue();
        var userId = userIdElement.GetInt32();

        // 2. Get user by email
        var getByEmailResponse = await client.GetAsync($"/api/users/by-email?email={email}");
        getByEmailResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var getUserContent = await getByEmailResponse.Content.ReadFromJsonAsync<JsonElement>();
        getUserContent.TryGetProperty("user", out var userElement).Should().BeTrue();
        userElement.GetProperty("email").GetString().Should().Be(email);
        userElement.GetProperty("nickname").GetString().Should().Be("usercrud");

        // 3. Get user by ID
        var getByIdResponse = await client.GetAsync($"/api/users/{userId}");
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 4. Update user
        var updateDto = new
        {
            nickname = "updatedcrud",
            name = "Updated",
            lastname = "CRUD User",
            email = "updated" + email,
            role = "admin",
            status = "active",
            emailConfirmed = true
        };

        var updateResponse = await client.PatchAsJsonAsync($"/api/users/{userId}", updateDto);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 5. Verify update
        var getUpdatedResponse = await client.GetAsync($"/api/users/{userId}");
        getUpdatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updatedContent = await getUpdatedResponse.Content.ReadFromJsonAsync<JsonElement>();
        var userObj = updatedContent.GetProperty("user");
        userObj.GetProperty("nickname").GetString().Should().Be("updatedcrud");
        userObj.GetProperty("name").GetString().Should().Be("Updated");
        userObj.GetProperty("role").GetString().Should().Be("admin");

        // 6. Get all users (should include our user)
        var getAllResponse = await client.GetAsync("/api/users");
        getAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var allUsersContent = await getAllResponse.Content.ReadFromJsonAsync<JsonElement>();
        allUsersContent.TryGetProperty("users", out var usersArray).Should().BeTrue();
        usersArray.GetArrayLength().Should().BeGreaterThan(0);

        // 7. Delete user
        var deleteResponse = await client.DeleteAsync($"/api/users/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // 8. Verify deletion
        var getDeletedResponse = await client.GetAsync($"/api/users/{userId}");
        getDeletedResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreateUser_ShouldPreventDuplicateEmail()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var email = "duplicate@test.com";
        var userDto1 = new { nickname = "user1", name = "User", lastname = "One", email, password = "Password123!" };
        var userDto2 = new { nickname = "user2", name = "User", lastname = "Two", email, password = "Password123!" };

        await client.DeleteAsync($"/api/users/by-email/{email}");

        // Act
        var createResponse1 = await client.PostAsJsonAsync("/api/users", userDto1);
        var createResponse2 = await client.PostAsJsonAsync("/api/users", userDto2);

        // Assert
        createResponse1.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var conflictContent = await createResponse2.Content.ReadFromJsonAsync<JsonElement>();
        conflictContent.TryGetProperty("error", out var errorElement).Should().BeTrue();
        errorElement.GetString().Should().Contain("email");

        // Cleanup
        await client.DeleteAsync($"/api/users/by-email/{email}");
    }

    [Fact]
    public async Task CreateUser_ShouldPreventDuplicateNickname()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var nickname = "duplicatenick";
        var userDto1 = new { nickname, name = "User", lastname = "One", email = "user1@test.com", password = "Password123!" };
        var userDto2 = new { nickname, name = "User", lastname = "Two", email = "user2@test.com", password = "Password123!" };

        await client.DeleteAsync($"/api/users/by-email/user1@test.com");
        await client.DeleteAsync($"/api/users/by-email/user2@test.com");

        // Act
        var createResponse1 = await client.PostAsJsonAsync("/api/users", userDto1);
        var createResponse2 = await client.PostAsJsonAsync("/api/users", userDto2);

        // Assert
        createResponse1.StatusCode.Should().Be(HttpStatusCode.Created);
        createResponse2.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var conflictContent = await createResponse2.Content.ReadFromJsonAsync<JsonElement>();
        conflictContent.TryGetProperty("error", out var errorElement).Should().BeTrue();
        errorElement.GetString().Should().Contain("nickname");

        // Cleanup
        await client.DeleteAsync($"/api/users/by-email/user1@test.com");
    }

    [Theory]
    [InlineData("", "Test", "User", "test@example.com", "Password123!")]
    [InlineData("test", "", "User", "test@example.com", "Password123!")]
    [InlineData("test", "Test", "", "test@example.com", "Password123!")]
    [InlineData("test", "Test", "User", "", "Password123!")]
    [InlineData("test", "Test", "User", "test@example.com", "")]
    [InlineData("test", "Test", "User", "invalid-email", "Password123!")]
    public async Task CreateUser_ShouldValidateRequiredFields(string nickname, string name, string lastname, string email, string password)
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var userDto = new { nickname, name, lastname, email, password };

        // Act
        var response = await client.PostAsJsonAsync("/api/users", userDto);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateUser_ShouldPreventEmailConflict()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();
        var user1Email = "updateuser1@test.com";
        var user2Email = "updateuser2@test.com";

        var userDto1 = new { nickname = "updateuser1", name = "Update", lastname = "User1", email = user1Email, password = "Password123!" };
        var userDto2 = new { nickname = "updateuser2", name = "Update", lastname = "User2", email = user2Email, password = "Password123!" };

        await client.DeleteAsync($"/api/users/by-email/{user1Email}");
        await client.DeleteAsync($"/api/users/by-email/{user2Email}");

        var createResponse1 = await client.PostAsJsonAsync("/api/users", userDto1);
        await client.PostAsJsonAsync("/api/users", userDto2);

        var createContent1 = await createResponse1.Content.ReadFromJsonAsync<JsonElement>();
        var user1Id = createContent1.GetProperty("id").GetInt32();

        // Act - try to update user1 with user2's email
        var updateDto = new { email = user2Email };
        var updateResponse = await client.PatchAsJsonAsync($"/api/users/{user1Id}", updateDto);

        // Assert
        updateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        // Cleanup
        await client.DeleteAsync($"/api/users/by-email/{user1Email}");
        await client.DeleteAsync($"/api/users/by-email/{user2Email}");
    }

    [Fact]
    public async Task DeleteAllData_ShouldCleanDatabase()
    {
        // Arrange
        var client = _factory.CreateAuthenticatedClient();

        // Create some test data
        var userDto1 = new { nickname = "deleteall1", name = "Delete", lastname = "All1", email = "deleteall1@test.com", password = "Password123!" };
        var userDto2 = new { nickname = "deleteall2", name = "Delete", lastname = "All2", email = "deleteall2@test.com", password = "Password123!" };

        await client.PostAsJsonAsync("/api/users-with-preferences", userDto1);
        await client.PostAsJsonAsync("/api/users-with-preferences", userDto2);

        // Create sessions by logging in
        await client.PostAsJsonAsync("/api/auth/login", new { email = "deleteall1@test.com", password = "Password123!" });
        await client.PostAsJsonAsync("/api/auth/login", new { email = "deleteall2@test.com", password = "Password123!" });

        // Act
        var deleteAllResponse = await client.DeleteAsync("/api/users/all-data");

        // Assert
        deleteAllResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Verify all data was deleted (puede quedar el usuario de prueba global del factory)
        var getUsersResponse = await client.GetAsync("/api/users");
        getUsersResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var usersContent = await getUsersResponse.Content.ReadFromJsonAsync<JsonElement>();
        usersContent.TryGetProperty("users", out var usersArray).Should().BeTrue();
        usersArray.GetArrayLength().Should().BeLessThanOrEqualTo(1);

        var getSessionsResponse = await client.GetAsync("/api/sessions");
        getSessionsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var sessionsContent = await getSessionsResponse.Content.ReadFromJsonAsync<JsonElement>();
        sessionsContent.TryGetProperty("sessions", out var sessionsArray).Should().BeTrue();
        sessionsArray.GetArrayLength().Should().BeLessThanOrEqualTo(1); // Puede quedar la sesión del admin
    }
}