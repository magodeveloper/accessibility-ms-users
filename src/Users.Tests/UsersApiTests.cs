using Xunit;
using System.Net;
using Users.Domain;
using Users.Application;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Users.Tests;

public class UsersApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    public UsersApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CrearUsuario_RetornaCreatedYObtieneUsuario()
    {
        var client = _factory.CreateClient();
        var dto = new UserCreateDto(
            Nickname: "testuser",
            Name: "Test",
            Lastname: "User",
            Email: "testuser@email.com",
            Password: "Test1234!"
        );
        var resp = await client.PostAsJsonAsync("/api/v1/users", dto);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
        var location = resp.Headers.Location;
        Assert.NotNull(location);
        var getResp = await client.GetAsync(location);
        Assert.Equal(HttpStatusCode.OK, getResp.StatusCode);
    }

    [Fact]
    public async Task LoginUsuario_RetornaToken()
    {
        var client = _factory.CreateClient();
        // Crear usuario primero
        var dto = new UserCreateDto("loginuser", "Login", "User", "login@email.com", "Test1234!");
        await client.PostAsJsonAsync("/api/v1/users", dto);
        // Login
        var login = new LoginDto("login@email.com", "Test1234!");
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", login);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var data = await resp.Content.ReadFromJsonAsync<LoginResponseDto>();
        Assert.NotNull(data);
        Assert.False(string.IsNullOrEmpty(data!.Token));
    }

    [Fact]
    public async Task ListarUsuarios_DevuelveListaIncluyendoNuevo()
    {
        var client = _factory.CreateClient();
        var dto = new UserCreateDto("listuser", "List", "User", "list@email.com", "Test1234!");
        await client.PostAsJsonAsync("/api/v1/users", dto);
        var resp = await client.GetAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        var list = await resp.Content.ReadFromJsonAsync<UserReadDto[]>();
        Assert.Contains(list!, u => u.Email == "list@email.com");
    }
    [Fact]
    public async Task CrearUsuario_Duplicado_RetornaConflict()
    {
        var client = _factory.CreateClient();
        var dto = new UserCreateDto("dupuser", "Dup", "User", "dup@email.com", "Test1234!");
        var resp1 = await client.PostAsJsonAsync("/api/v1/users", dto);
        Assert.Equal(HttpStatusCode.Created, resp1.StatusCode);
        var resp2 = await client.PostAsJsonAsync("/api/v1/users", dto);
        Assert.Equal(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [Fact]
    public async Task Login_Incorrecto_RetornaUnauthorized()
    {
        var client = _factory.CreateClient();
        var dto = new UserCreateDto("badlogin", "Bad", "Login", "badlogin@email.com", "Test1234!");
        await client.PostAsJsonAsync("/api/v1/users", dto);
        var login = new LoginDto("badlogin@email.com", "WrongPassword");
        var resp = await client.PostAsJsonAsync("/api/v1/auth/login", login);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task CrearYObtenerPreferenciasUsuario()
    {
        var client = _factory.CreateClient();
        var userDto = new UserCreateDto("prefuser", "Pref", "User", "pref@email.com", "Test1234!");
        var userResp = await client.PostAsJsonAsync("/api/v1/users", userDto);
        var location = userResp.Headers.Location;
        var getResp = await client.GetAsync(location!);
        var user = await getResp.Content.ReadFromJsonAsync<UserReadDto>();
        var prefDto = new PreferenceCreateDto(user!.Id, "2.1", "AA", "es", "dark", "pdf", true, "detailed", 18);
        var prefResp = await client.PostAsJsonAsync("/api/v1/preferences", prefDto);
        Assert.Equal(HttpStatusCode.Created, prefResp.StatusCode);

        var getPrefResp = await client.GetAsync($"/api/v1/preferences/by-user/{user.Id}");
        Assert.Equal(HttpStatusCode.OK, getPrefResp.StatusCode);
    }

    [Fact]
    public async Task CrearYEliminarSesion()
    {
        var client = _factory.CreateClient();
        var dto = new UserCreateDto("sessuser", "Sess", "User", "sess@email.com", "Test1234!");
        await client.PostAsJsonAsync("/api/v1/users", dto);
        var login = new LoginDto("sess@email.com", "Test1234!");
        await client.PostAsJsonAsync("/api/v1/auth/login", login);

        // Buscar sesión creada
        await client.GetAsync("/api/v1/users");

        // Buscar sesiones (no hay endpoint de listar, así que solo probamos delete por id inválido)
        var delResp = await client.DeleteAsync("/api/v1/sessions/99999");
        Assert.Equal(HttpStatusCode.NotFound, delResp.StatusCode);
    }
}