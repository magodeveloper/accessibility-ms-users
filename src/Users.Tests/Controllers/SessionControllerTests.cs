using Moq;
using FluentAssertions;
using Users.Application.Dtos;
using Users.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Users.Application.Services.Session;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Services.UserContext;

namespace Users.Tests.Controllers;

public class SessionControllerTests : IDisposable
{
    private readonly Mock<ISessionService> _mockSessionService;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly SessionController _controller;
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed = false;

    public SessionControllerTests()
    {
        var services = new ServiceCollection();
        _mockSessionService = new Mock<ISessionService>();
        _mockUserContext = new Mock<IUserContext>();

        services.AddSingleton(_mockSessionService.Object);
        services.AddSingleton(_mockUserContext.Object);
        _serviceProvider = services.BuildServiceProvider();

        // Configurar mock IUserContext - usuario autenticado como admin
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUserContext.Setup(x => x.UserId).Returns(1);
        _mockUserContext.Setup(x => x.Email).Returns("test@example.com");
        _mockUserContext.Setup(x => x.Role).Returns("Admin");
        _mockUserContext.Setup(x => x.IsAdmin).Returns(true);
        _mockUserContext.Setup(x => x.UserName).Returns("TestUser");

        _controller = new SessionController(_mockSessionService.Object, _mockUserContext.Object);

        // Setup mock HTTP context para LanguageHelper
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["Accept-Language"] = "es";
        _controller.ControllerContext = new ControllerContext()
        {
            HttpContext = httpContext
        };
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _serviceProvider?.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task GetByUserId_WithExistingSessions_ShouldReturnOkWithSessions()
    {
        // Arrange
        var userId = 1;
        var sessions = new List<SessionReadDto>
        {
            new SessionReadDto(1, 1, "token1", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(1), null),
            new SessionReadDto(2, 1, "token2", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(2), null)
        };

        _mockSessionService.Setup(x => x.GetSessionsByUserIdAsync(userId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetByUserId(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();

        _mockSessionService.Verify(x => x.GetSessionsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByUserId_WithNoSessions_ShouldReturnNotFound()
    {
        // Arrange
        var userId = 999;
        var emptySessions = new List<SessionReadDto>();

        _mockSessionService.Setup(x => x.GetSessionsByUserIdAsync(userId))
            .ReturnsAsync(emptySessions);

        // Act
        var result = await _controller.GetByUserId(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);

        _mockSessionService.Verify(x => x.GetSessionsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByUserId_WithNullSessions_ShouldReturnNotFound()
    {
        // Arrange
        var userId = 999;

        _mockSessionService.Setup(x => x.GetSessionsByUserIdAsync(userId))
            .ReturnsAsync((IEnumerable<SessionReadDto>)null!);

        // Act
        var result = await _controller.GetByUserId(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);

        _mockSessionService.Verify(x => x.GetSessionsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetAll_ShouldReturnOkWithAllSessions()
    {
        // Arrange
        var sessions = new List<SessionReadDto>
        {
            new SessionReadDto(1, 1, "token1", DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddHours(1), null),
            new SessionReadDto(2, 2, "token2", DateTime.UtcNow.AddHours(-2), DateTime.UtcNow.AddHours(2), null),
            new SessionReadDto(3, 3, "token3", DateTime.UtcNow.AddDays(-2), DateTime.UtcNow, null)
        };

        _mockSessionService.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();

        _mockSessionService.Verify(x => x.GetAllSessionsAsync(), Times.Once);
    }

    [Fact]
    public async Task GetAll_WithEmptyDatabase_ShouldReturnOkWithEmptyList()
    {
        // Arrange
        var emptySessions = new List<SessionReadDto>();

        _mockSessionService.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(emptySessions);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();

        _mockSessionService.Verify(x => x.GetAllSessionsAsync(), Times.Once);
    }

    [Fact]
    public async Task Delete_WithExistingSession_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var sessionId = 1;

        _mockSessionService.Setup(x => x.DeleteSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(sessionId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();

        _mockSessionService.Verify(x => x.DeleteSessionAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentSession_ShouldReturnNotFound()
    {
        // Arrange
        var sessionId = 999;

        _mockSessionService.Setup(x => x.DeleteSessionAsync(sessionId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(sessionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);

        _mockSessionService.Verify(x => x.DeleteSessionAsync(sessionId), Times.Once);
    }

    [Fact]
    public async Task DeleteByUserId_WithExistingSessions_ShouldReturnOkWithSuccessMessage()
    {
        // Arrange
        var userId = 1;

        _mockSessionService.Setup(x => x.DeleteSessionsByUserIdAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.DeleteByUserId(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();

        _mockSessionService.Verify(x => x.DeleteSessionsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteByUserId_WithNoSessionsForUser_ShouldReturnNotFound()
    {
        // Arrange
        var userId = 999;

        _mockSessionService.Setup(x => x.DeleteSessionsByUserIdAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteByUserId(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);

        _mockSessionService.Verify(x => x.DeleteSessionsByUserIdAsync(userId), Times.Once);
    }

    [Fact]
    public async Task GetByUserId_ShouldCallServiceWithCorrectParameter()
    {
        // Arrange
        var userId = 42;
        var sessions = new List<SessionReadDto>
        {
            new SessionReadDto(1, userId, "token", DateTime.UtcNow, DateTime.UtcNow.AddHours(1), null)
        };

        _mockSessionService.Setup(x => x.GetSessionsByUserIdAsync(userId))
            .ReturnsAsync(sessions);

        // Act
        await _controller.GetByUserId(userId);

        // Assert
        _mockSessionService.Verify(x => x.GetSessionsByUserIdAsync(userId), Times.Once);
        _mockSessionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldCallServiceWithCorrectParameter()
    {
        // Arrange
        var sessionId = 42;

        _mockSessionService.Setup(x => x.DeleteSessionAsync(sessionId))
            .ReturnsAsync(true);

        // Act
        await _controller.Delete(sessionId);

        // Assert
        _mockSessionService.Verify(x => x.DeleteSessionAsync(sessionId), Times.Once);
        _mockSessionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteByUserId_ShouldCallServiceWithCorrectParameter()
    {
        // Arrange
        var userId = 42;

        _mockSessionService.Setup(x => x.DeleteSessionsByUserIdAsync(userId))
            .ReturnsAsync(true);

        // Act
        await _controller.DeleteByUserId(userId);

        // Assert
        _mockSessionService.Verify(x => x.DeleteSessionsByUserIdAsync(userId), Times.Once);
        _mockSessionService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAll_ShouldNotRequireParameters()
    {
        // Arrange
        var sessions = new List<SessionReadDto>();

        _mockSessionService.Setup(x => x.GetAllSessionsAsync())
            .ReturnsAsync(sessions);

        // Act
        await _controller.GetAll();

        // Assert
        _mockSessionService.Verify(x => x.GetAllSessionsAsync(), Times.Once);
        _mockSessionService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task GetByUserId_WithVariousUserIds_ShouldHandleAllValues(int userId)
    {
        // Arrange
        var sessions = new List<SessionReadDto>();

        _mockSessionService.Setup(x => x.GetSessionsByUserIdAsync(userId))
            .ReturnsAsync(sessions);

        // Act
        var result = await _controller.GetByUserId(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockSessionService.Verify(x => x.GetSessionsByUserIdAsync(userId), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Delete_WithVariousSessionIds_ShouldHandleAllValues(int sessionId)
    {
        // Arrange
        _mockSessionService.Setup(x => x.DeleteSessionAsync(sessionId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(sessionId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockSessionService.Verify(x => x.DeleteSessionAsync(sessionId), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task DeleteByUserId_WithVariousUserIds_ShouldHandleAllValues(int userId)
    {
        // Arrange
        _mockSessionService.Setup(x => x.DeleteSessionsByUserIdAsync(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DeleteByUserId(userId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockSessionService.Verify(x => x.DeleteSessionsByUserIdAsync(userId), Times.Once);
    }

    // ===== Tests de autenticación =====

    [Fact]
    public async Task GetAll_NotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task GetByUserId_NotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
        var userId = 1;

        // Act
        var result = await _controller.GetByUserId(userId);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task Delete_NotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
        var sessionId = 1;

        // Act
        var result = await _controller.Delete(sessionId);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }

    [Fact]
    public async Task DeleteByUserId_NotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
        var userId = 1;

        // Act
        var result = await _controller.DeleteByUserId(userId);

        // Assert
        result.Should().BeOfType<UnauthorizedObjectResult>();
    }
}