using Moq;
using Xunit;
using Users.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Users.Application.Services.UserContext;

namespace Users.Tests.Middleware;

public class UserContextMiddlewareTests
{
    private readonly Mock<ILogger<UserContextMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly UserContextMiddleware _middleware;

    public UserContextMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<UserContextMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
        _middleware = new UserContextMiddleware(_mockNext.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task InvokeAsync_WithValidHeaders_ShouldPopulateUserContext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "123";
        context.Request.Headers["X-User-Email"] = "test@example.com";
        context.Request.Headers["X-User-Role"] = "Admin";
        context.Request.Headers["X-User-Name"] = "Test User";

        var userContext = new UserContext();

        // Act
        await _middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(123, userContext.UserId);
        Assert.Equal("test@example.com", userContext.Email);
        Assert.Equal("Admin", userContext.Role);
        Assert.Equal("Test User", userContext.UserName);
        Assert.True(userContext.IsAuthenticated);
        Assert.True(userContext.IsAdmin);

        _mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithoutHeaders_ShouldLeaveUserContextEmpty()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var userContext = new UserContext();

        // Act
        await _middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(0, userContext.UserId);
        Assert.Equal(string.Empty, userContext.Email);
        Assert.Equal(string.Empty, userContext.Role);
        Assert.Equal(string.Empty, userContext.UserName);
        Assert.False(userContext.IsAuthenticated);
        Assert.False(userContext.IsAdmin);

        _mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidUserId_ShouldNotPopulateUserId()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "invalid";
        context.Request.Headers["X-User-Email"] = "test@example.com";

        var userContext = new UserContext();

        // Act
        await _middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(0, userContext.UserId);
        Assert.Equal(string.Empty, userContext.Email); // No debe poblar email si userId es inválido
        Assert.False(userContext.IsAuthenticated);

        _mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithStandardRole_ShouldNotBeAdmin()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "456";
        context.Request.Headers["X-User-Email"] = "user@example.com";
        context.Request.Headers["X-User-Role"] = "Standard";
        context.Request.Headers["X-User-Name"] = "Standard User";

        var userContext = new UserContext();

        // Act
        await _middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(456, userContext.UserId);
        Assert.Equal("Standard", userContext.Role);
        Assert.True(userContext.IsAuthenticated);
        Assert.False(userContext.IsAdmin);

        _mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithPartialHeaders_ShouldPopulateAvailableData()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Request.Headers["X-User-Id"] = "789";
        context.Request.Headers["X-User-Email"] = "partial@example.com";
        // Sin X-User-Role ni X-User-Name

        var userContext = new UserContext();

        // Act
        await _middleware.InvokeAsync(context, userContext);

        // Assert
        Assert.Equal(789, userContext.UserId);
        Assert.Equal("partial@example.com", userContext.Email);
        Assert.Equal(string.Empty, userContext.Role);
        Assert.Equal(string.Empty, userContext.UserName);
        Assert.True(userContext.IsAuthenticated);
        Assert.False(userContext.IsAdmin);

        _mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ShouldAlwaysCallNextMiddleware()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var userContext = new UserContext();

        // Act
        await _middleware.InvokeAsync(context, userContext);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
    }

    [Fact]
    public void UseUserContext_ShouldRegisterMiddleware()
    {
        // Arrange
        var mockApp = new Mock<Microsoft.AspNetCore.Builder.IApplicationBuilder>();
        mockApp.Setup(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
               .Returns(mockApp.Object);

        // Act
        var result = UserContextMiddlewareExtensions.UseUserContext(mockApp.Object);

        // Assert
        Assert.NotNull(result);
        // Verificar que se llamó al método Use (que es lo que hace UseMiddleware internamente)
        mockApp.Verify(a => a.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }
}
