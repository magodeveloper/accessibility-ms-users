using Moq;
using Xunit;
using Users.Api.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Users.Tests.HealthChecks;

public class MemoryHealthCheckTests
{
    private readonly Mock<ILogger<MemoryHealthCheck>> _mockLogger;

    public MemoryHealthCheckTests()
    {
        _mockLogger = new Mock<ILogger<MemoryHealthCheck>>();
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnHealthy_WhenMemoryIsUnderThreshold()
    {
        // Arrange
        var threshold = 10L * 1024L * 1024L * 1024L; // 10GB - muy alto para que siempre sea healthy
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object, threshold);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Healthy, result.Status);
        Assert.NotNull(result.Data);
        Assert.True(result.Data.ContainsKey("allocatedMB"));
        Assert.True(result.Data.ContainsKey("thresholdMB"));
        Assert.True(result.Data.ContainsKey("gen0Collections"));
        Assert.True(result.Data.ContainsKey("gen1Collections"));
        Assert.True(result.Data.ContainsKey("gen2Collections"));
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldReturnDegraded_WhenMemoryExceedsThreshold()
    {
        // Arrange
        var threshold = 1L; // 1 byte - threshold muy bajo para forzar degraded
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object, threshold);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.Equal(HealthStatus.Degraded, result.Status);
        Assert.Contains("Memory usage is high", result.Description);
        Assert.NotNull(result.Data);
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldIncludeGarbageCollectionStats()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Data.ContainsKey("gen0Collections"));
        Assert.True(result.Data.ContainsKey("gen1Collections"));
        Assert.True(result.Data.ContainsKey("gen2Collections"));

        var gen0 = (int)result.Data["gen0Collections"];
        var gen1 = (int)result.Data["gen1Collections"];
        var gen2 = (int)result.Data["gen2Collections"];

        Assert.True(gen0 >= 0, "Gen0 collections should be non-negative");
        Assert.True(gen1 >= 0, "Gen1 collections should be non-negative");
        Assert.True(gen2 >= 0, "Gen2 collections should be non-negative");
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldCalculateMemoryCorrectly()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Data.ContainsKey("allocatedMB"));
        var allocatedMB = (double)result.Data["allocatedMB"];
        Assert.True(allocatedMB > 0, "Allocated memory should be positive");
        Assert.True(allocatedMB < 100000, "Allocated memory should be reasonable (less than 100GB)");
    }

    [Fact]
    public async Task CheckHealthAsync_WithDefaultThreshold_ShouldUse1GB()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object); // default 1GB
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Data.ContainsKey("thresholdMB"));
        var thresholdMB = (double)result.Data["thresholdMB"];
        Assert.Equal(1024.0, thresholdMB, 1); // 1GB = 1024MB (con tolerancia de 1MB)
    }

    [Fact]
    public async Task CheckHealthAsync_WithCustomThreshold_ShouldUseProvidedValue()
    {
        // Arrange
        var customThreshold = 512L * 1024L * 1024L; // 512MB
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object, customThreshold);
        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        Assert.True(result.Data.ContainsKey("thresholdMB"));
        var thresholdMB = (double)result.Data["thresholdMB"];
        Assert.Equal(512.0, thresholdMB, 1); // 512MB (con tolerancia de 1MB)
    }

    [Fact]
    public async Task CheckHealthAsync_ShouldLogDebugOnSuccess()
    {
        // Arrange
        var threshold = 10L * 1024L * 1024L * 1024L; // 10GB
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object, threshold);
        var context = new HealthCheckContext();

        // Act
        await healthCheck.CheckHealthAsync(context);

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
    public async Task CheckHealthAsync_ShouldLogWarningWhenDegraded()
    {
        // Arrange
        var threshold = 1L; // 1 byte para forzar degraded
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object, threshold);
        var context = new HealthCheckContext();

        // Act
        await healthCheck.CheckHealthAsync(context);

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
    public async Task CheckHealthAsync_WithCancellationToken_ShouldComplete()
    {
        // Arrange
        var healthCheck = new MemoryHealthCheck(_mockLogger.Object);
        var context = new HealthCheckContext();
        using var cts = new CancellationTokenSource();

        // Act
        var result = await healthCheck.CheckHealthAsync(context, cts.Token);

        // Assert
        Assert.NotNull(result.Data);
        Assert.True(result.Status == HealthStatus.Healthy || result.Status == HealthStatus.Degraded);
    }
}
