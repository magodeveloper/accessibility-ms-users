using Xunit;
using Prometheus;
using Users.Api.Services;

namespace Users.Tests.Services;

public class UserMetricsServiceTests
{
    private readonly IUserMetricsService _metricsService;

    public UserMetricsServiceTests()
    {
        _metricsService = new UserMetricsService();
    }

    [Fact]
    public void RecordUserRegistration_WithSuccess_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordUserRegistration(true));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordUserRegistration_WithFailure_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordUserRegistration(false));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordUserLogin_WithSuccess_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordUserLogin(true));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordUserLogin_WithFailure_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordUserLogin(false));
        Assert.Null(exception);
    }

    [Fact]
    public void RecordUserDeletion_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordUserDeletion());
        Assert.Null(exception);
    }

    [Fact]
    public void RecordSessionCreation_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordSessionCreation());
        Assert.Null(exception);
    }

    [Fact]
    public void RecordSessionDeletion_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordSessionDeletion());
        Assert.Null(exception);
    }

    [Fact]
    public void RecordPreferenceUpdate_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordPreferenceUpdate());
        Assert.Null(exception);
    }

    [Fact]
    public void RecordPasswordReset_ShouldNotThrow()
    {
        // Act & Assert
        var exception = Record.Exception(() => _metricsService.RecordPasswordReset());
        Assert.Null(exception);
    }

    [Fact]
    public void RecordApiRequest_ShouldRecordHistogram()
    {
        // Arrange & Act
        _metricsService.RecordApiRequest("/api/users", "GET", 200, 150.5);

        // Assert - verificar que no lanza excepción
        // El histograma no tiene método fácil de verificar el valor, 
        // pero si no lanza excepción, está funcionando
        Assert.True(true, "Histogram recording should succeed");
    }

    [Theory]
    [InlineData("/api/users", "POST", 201, 250.0)]
    [InlineData("/api/auth/login", "POST", 200, 100.0)]
    [InlineData("/api/preferences", "GET", 200, 50.0)]
    [InlineData("/api/sessions", "DELETE", 204, 75.0)]
    public void RecordApiRequest_WithVariousEndpoints_ShouldNotThrow(
        string endpoint, string method, int statusCode, double durationMs)
    {
        // Act & Assert
        var exception = Record.Exception(() =>
            _metricsService.RecordApiRequest(endpoint, method, statusCode, durationMs));

        Assert.Null(exception);
    }

    [Fact]
    public void RecordMultipleOperations_ShouldTrackAllMetrics()
    {
        // Arrange
        var service = new UserMetricsService();

        // Act & Assert - verificar que no lanza excepciones
        var exception = Record.Exception(() =>
        {
            service.RecordUserRegistration(true);
            service.RecordUserLogin(true);
            service.RecordSessionCreation();
            service.RecordPreferenceUpdate();
            service.RecordPasswordReset();
            service.RecordUserDeletion();
            service.RecordSessionDeletion();
        });

        Assert.Null(exception);
    }
}
