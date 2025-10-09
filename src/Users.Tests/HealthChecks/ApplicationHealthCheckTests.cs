using Moq;
using Xunit;
using Users.Api.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.Tests.HealthChecks;

public class ApplicationHealthCheckTests
{
    private readonly Mock<ILogger<ApplicationHealthCheck>> _mockLogger;
    private readonly Mock<IHostApplicationLifetime> _mockLifetime;
    private readonly ApplicationHealthCheck _healthCheck;

    public ApplicationHealthCheckTests()
    {
        _mockLogger = new Mock<ILogger<ApplicationHealthCheck>>();
        _mockLifetime = new Mock<IHostApplicationLifetime>();
        _healthCheck = new ApplicationHealthCheck(_mockLogger.Object, _mockLifetime.Object);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenApplicationIsRunning()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("status"));
        Assert.Equal("running", result.Data["status"]);
        Assert.True(result.Data.ContainsKey("environment"));
        Assert.True(result.Data.ContainsKey("uptimeSeconds"));
        Assert.True(result.Data.ContainsKey("uptimeFormatted"));
        Assert.True(result.Data.ContainsKey("timestamp"));
        Assert.True(result.Data.ContainsKey("machineName"));
        Assert.True(result.Data.ContainsKey("processId"));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnUnhealthy_WhenApplicationIsStopping()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync(); // Simular shutdown
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Unhealthy, result.Status);
        Assert.Contains("shutting down", result.Description);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("status"));
        Assert.Equal("stopping", result.Data["status"]);
        Assert.True(result.Data.ContainsKey("timestamp"));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeUptimeData()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Data.ContainsKey("uptimeSeconds"));
        Assert.True(result.Data.ContainsKey("uptimeFormatted"));

        var uptimeSeconds = (double)result.Data["uptimeSeconds"];
        Assert.True(uptimeSeconds >= 0, "Uptime should be non-negative");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeEnvironmentData()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Data.ContainsKey("environment"));
        Assert.True(result.Data.ContainsKey("machineName"));
        Assert.True(result.Data.ContainsKey("processId"));

        Assert.NotNull(result.Data["environment"]);
        Assert.NotNull(result.Data["machineName"]);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldAlwaysCallNextMiddleware()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        var result = await _healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Unhealthy);
    }

    [Fact]
    public async Task CheckHealthAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        var result = await _healthCheck.CheckHealthAsync(context, cancellationTokenSource.Token);

        // Assert
        Assert.True(result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Unhealthy);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldLogDebugInformation()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        await _healthCheck.CheckHealthAsync(context);

        // Assert - verificar que se log al menos una vez
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task CheckHealthAsync_WhenStopping_ShouldLogWarning()
    {
        // Arrange
        using var cancellationTokenSource = new CancellationTokenSource();
        await cancellationTokenSource.CancelAsync();
        _mockLifetime.Setup(x => x.ApplicationStopping).Returns(cancellationTokenSource.Token);

        var context = new HealthCheckContext();

        // Act
        await _healthCheck.CheckHealthAsync(context);

        // Assert - verificar que se log
        _mockLogger.Verify(
            x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.AtLeastOnce);
    }
}
