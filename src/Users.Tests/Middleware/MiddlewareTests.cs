using Moq;
using Xunit;
using FluentAssertions;
using Users.Api.Middleware;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Users.Application.Services.UserContext;

namespace Users.Tests.Middleware;

/// <summary>
/// Tests unitarios para los Middlewares del microservicio Users
/// </summary>
public class MiddlewareTests
{
    #region UserContextMiddleware Tests

    [Fact]
    public async Task UserContextMiddleware_WithValidHeaders_PopulatesUserContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "42";
        context.Request.Headers["X-User-Email"] = "test@example.com";
        context.Request.Headers["X-User-Role"] = "Admin";
        context.Request.Headers["X-User-Name"] = "TestUser";

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        userContext.UserId.Should().Be(42);
        userContext.Email.Should().Be("test@example.com");
        userContext.Role.Should().Be("Admin");
        userContext.UserName.Should().Be("TestUser");
        userContext.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task UserContextMiddleware_WithJwtClaims_PopulatesUserContext()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Crear claims simulando un JWT
        var claims = new List<Claim>
        {
            new Claim("sub", "99"),
            new Claim("email", "jwt@example.com"),
            new Claim("name", "JwtUser"),
            new Claim("role", "User")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        context.User = new ClaimsPrincipal(identity);

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        userContext.UserId.Should().Be(99);
        userContext.Email.Should().Be("jwt@example.com");
        userContext.UserName.Should().Be("JwtUser");
        userContext.Role.Should().Be("User");
        userContext.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task UserContextMiddleware_WithMicrosoftClaims_PopulatesUserContext()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Usar claims con nombres de esquema de Microsoft
        var claims = new List<Claim>
        {
            new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", "88"),
            new Claim("email", "microsoft@example.com"),
            new Claim("name", "MsUser"),
            new Claim("http://schemas.microsoft.com/ws/2008/06/identity/claims/role", "SuperUser")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        context.User = new ClaimsPrincipal(identity);

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        userContext.UserId.Should().Be(88);
        userContext.Email.Should().Be("microsoft@example.com");
        userContext.UserName.Should().Be("MsUser");
        userContext.Role.Should().Be("SuperUser");
        userContext.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task UserContextMiddleware_WithMissingHeaders_LeavesUserContextEmpty()
    {
        // Arrange
        var context = new DefaultHttpContext();
        // No agregar headers

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        userContext.IsAuthenticated.Should().BeFalse();
        userContext.UserId.Should().Be(0);
        userContext.Email.Should().BeEmpty();
    }

    [Fact]
    public async Task UserContextMiddleware_WithInvalidUserId_DoesNotPopulateContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "invalid-number";
        context.Request.Headers["X-User-Email"] = "test@example.com";

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        userContext.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task UserContextMiddleware_WithPartialHeaders_PopulatesAvailableFields()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "55";
        context.Request.Headers["X-User-Email"] = "partial@example.com";
        // No Role ni Name

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        userContext.UserId.Should().Be(55);
        userContext.Email.Should().Be("partial@example.com");
        userContext.Role.Should().BeEmpty();
        userContext.UserName.Should().BeEmpty();
        userContext.IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task UserContextMiddleware_WithEmptyHeaders_UsesEmptyStrings()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "77";
        context.Request.Headers["X-User-Email"] = "";
        context.Request.Headers["X-User-Role"] = "";

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        userContext.UserId.Should().Be(77);
        userContext.Email.Should().BeEmpty();
        userContext.Role.Should().BeEmpty();
    }

    [Fact]
    public async Task UserContextMiddleware_WithNonUserContextImplementation_ContinuesProcessing()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "100";

        // Usar un mock que NO es UserContext
        var mockUserContext = new Mock<IUserContext>();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var loggerMock = new Mock<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, mockUserContext.Object);

        // Assert
        nextCalled.Should().BeTrue();
        // Verificar que se logueó la advertencia
        loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("UserContext is not of type UserContext")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task UserContextMiddleware_WhenExceptionOccurs_ContinuesProcessing()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Agregar un header con ID inválido que podría causar problemas de parsing
        // pero el middleware debe manejar la excepción y continuar
        context.Request.Headers["X-User-Id"] = "not-a-number-but-very-long-string-that-causes-issues";

        // Crear un UserContext real que funcione
        var userContext = new UserContext();

        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var loggerMock = new Mock<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, loggerMock.Object);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue("el middleware debe continuar incluso si hay problemas parseando headers");
        // El userContext no debe estar autenticado porque el parsing falló
        userContext.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task UserContextMiddleware_PrefersHeadersOverJwt()
    {
        // Arrange
        var context = new DefaultHttpContext();

        // Agregar AMBOS: headers y JWT claims
        context.Request.Headers["X-User-Id"] = "1";
        context.Request.Headers["X-User-Email"] = "header@example.com";

        var claims = new List<Claim>
        {
            new Claim("sub", "999"),
            new Claim("email", "jwt@example.com")
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        context.User = new ClaimsPrincipal(identity);

        var userContext = new UserContext();
        var nextCalled = false;
        RequestDelegate next = (HttpContext ctx) =>
        {
            nextCalled = true;
            return Task.CompletedTask;
        };

        var logger = Mock.Of<ILogger<UserContextMiddleware>>();
        var middleware = new UserContextMiddleware(next, logger);

        // Act
        await middleware.InvokeAsync(context, userContext);

        // Assert
        nextCalled.Should().BeTrue();
        // Los headers deben tener prioridad sobre JWT
        userContext.UserId.Should().Be(1, "headers have priority over JWT");
        userContext.Email.Should().Be("header@example.com", "headers have priority over JWT");
    }

    [Fact]
    public void UseUserContext_RegistersMiddleware()
    {
        // Arrange
        var mockApplicationBuilder = new Mock<Microsoft.AspNetCore.Builder.IApplicationBuilder>();
        mockApplicationBuilder
            .Setup(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(mockApplicationBuilder.Object);

        // Act
        var result = mockApplicationBuilder.Object.UseUserContext();

        // Assert
        result.Should().NotBeNull();
        mockApplicationBuilder.Verify(x => x.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    #endregion
}
