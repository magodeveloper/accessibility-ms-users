using Moq;
using Xunit;
using System.Text.Json;
using Users.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;

namespace Users.Tests.Middleware;

public class GatewaySecretValidationMiddlewareTests
{
    private readonly Mock<ILogger<GatewaySecretValidationMiddleware>> _mockLogger;
    private readonly Mock<RequestDelegate> _mockNext;
    private readonly Mock<IWebHostEnvironment> _mockEnvironment;

    public GatewaySecretValidationMiddlewareTests()
    {
        _mockLogger = new Mock<ILogger<GatewaySecretValidationMiddleware>>();
        _mockNext = new Mock<RequestDelegate>();
        _mockEnvironment = new Mock<IWebHostEnvironment>();
    }

    private static IConfiguration CreateConfiguration(string? gatewaySecret = null)
    {
        var configBuilder = new ConfigurationBuilder();

        if (gatewaySecret != null)
        {
            configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Gateway:Secret"] = gatewaySecret
            });
        }

        return configBuilder.Build();
    }

    [Fact]
    public async Task InvokeAsync_InTestEnvironment_ShouldSkipValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("TestEnvironment");
        var config = CreateConfiguration("test-secret");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.NotEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithoutSecretConfigured_ShouldSkipValidation()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var config = CreateConfiguration(); // No secret configured
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.NotEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public void Constructor_WithoutSecretConfigured_ShouldLogWarning()
    {
        // Arrange & Act
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var config = CreateConfiguration(); // No secret configured

        _ = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Gateway:Secret not configured")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithoutGatewaySecretHeader_ShouldReturn403()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var config = CreateConfiguration("my-secret-key");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        _mockNext.Verify(next => next(context), Times.Never);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);

        Assert.NotNull(jsonResponse);
        Assert.Equal("Forbidden", jsonResponse["error"].GetString());
        Assert.Contains("Direct access to microservice is not allowed", jsonResponse["message"].GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidGatewaySecret_ShouldReturn403()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var config = CreateConfiguration("correct-secret");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Gateway-Secret"] = "wrong-secret";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        _mockNext.Verify(next => next(context), Times.Never);

        // Verify response body
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        var reader = new StreamReader(context.Response.Body);
        var responseBody = await reader.ReadToEndAsync();
        var jsonResponse = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(responseBody);

        Assert.NotNull(jsonResponse);
        Assert.Equal("Forbidden", jsonResponse["error"].GetString());
        Assert.Contains("Invalid Gateway secret", jsonResponse["message"].GetString());
    }

    [Fact]
    public async Task InvokeAsync_WithValidGatewaySecret_ShouldCallNext()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var secretKey = "my-valid-secret-key";
        var config = CreateConfiguration(secretKey);
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Gateway-Secret"] = secretKey;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.NotEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_WithValidGatewaySecret_ShouldLogDebug()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Development");
        var secretKey = "my-valid-secret-key";
        var config = CreateConfiguration(secretKey);
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Gateway-Secret"] = secretKey;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Debug,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Gateway secret validated successfully")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithMissingHeader_ShouldLogWarning()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var config = CreateConfiguration("my-secret");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Missing X-Gateway-Secret header")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_WithInvalidSecret_ShouldLogWarning()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var config = CreateConfiguration("correct-secret");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Gateway-Secret"] = "wrong-secret";
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => o.ToString()!.Contains("Invalid X-Gateway-Secret header")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task InvokeAsync_ReadingSecretFromEnvironmentVariable_ShouldWork()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GATEWAY_SECRET"] = "env-secret-key"
        });
        var config = configBuilder.Build();

        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Gateway-Secret"] = "env-secret-key";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _mockNext.Verify(next => next(context), Times.Once);
        Assert.NotEqual(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public void UseGatewaySecretValidation_ShouldRegisterMiddleware()
    {
        // Arrange
        var mockAppBuilder = new Mock<IApplicationBuilder>();
        mockAppBuilder.Setup(app => app.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()))
            .Returns(mockAppBuilder.Object);

        // Act
        var result = mockAppBuilder.Object.UseGatewaySecretValidation();

        // Assert
        Assert.NotNull(result);
        mockAppBuilder.Verify(app => app.Use(It.IsAny<Func<RequestDelegate, RequestDelegate>>()), Times.Once);
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task InvokeAsync_InNonTestEnvironments_ShouldEnforceValidation(string environmentName)
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns(environmentName);
        var config = CreateConfiguration("secret-key");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        // No X-Gateway-Secret header

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        _mockNext.Verify(next => next(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_WithEmptyGatewaySecret_ShouldReturn403()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var config = CreateConfiguration("my-secret");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Gateway-Secret"] = string.Empty;
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        _mockNext.Verify(next => next(context), Times.Never);
    }

    [Fact]
    public async Task InvokeAsync_CaseSensitiveSecret_ShouldEnforceExactMatch()
    {
        // Arrange
        _mockEnvironment.Setup(e => e.EnvironmentName).Returns("Production");
        var config = CreateConfiguration("MySecretKey");
        var middleware = new GatewaySecretValidationMiddleware(
            _mockNext.Object,
            _mockLogger.Object,
            config,
            _mockEnvironment.Object
        );

        var context = new DefaultHttpContext();
        context.Request.Headers["X-Gateway-Secret"] = "mysecretkey"; // Different case
        context.Response.Body = new MemoryStream();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        _mockNext.Verify(next => next(context), Times.Never);
    }
}
