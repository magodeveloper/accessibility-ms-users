using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Users.Api.Controllers;
using Users.Application.Dtos;
using Users.Application.Services.Preference;
using Users.Domain.Entities;

namespace Users.Tests.Controllers;

public class PreferenceControllerTests : IDisposable
{
    private readonly Mock<IPreferenceService> _mockPreferenceService;
    private readonly PreferenceController _controller;
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed = false;

    public PreferenceControllerTests()
    {
        var services = new ServiceCollection();
        _mockPreferenceService = new Mock<IPreferenceService>();

        services.AddSingleton(_mockPreferenceService.Object);
        _serviceProvider = services.BuildServiceProvider();

        _controller = new PreferenceController(_mockPreferenceService.Object);

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

    #region GetByUserEmail Tests

    [Fact]
    public async Task GetByUserEmail_WithExistingPreferences_ShouldReturnOkWithPreferences()
    {
        // Arrange
        var email = "test@example.com";
        var user = new User
        {
            Id = 1,
            Email = email,
            Nickname = "testnick",
            Name = "Test",
            Lastname = "User",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true
        };

        var preference = new Preference
        {
            Id = 1,
            UserId = 1,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.es,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate,
            FontSize = 14,
            User = user,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var preferences = new List<Preference>
        {
            preference
        };

        _mockPreferenceService.Setup(x => x.GetAllPreferencesAsync())
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.GetByUserEmail(email);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();

        _mockPreferenceService.Verify(x => x.GetAllPreferencesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByUserEmail_WithNonExistentEmail_ShouldReturnNotFound()
    {
        // Arrange
        var email = "nonexistent@example.com";
        var preferences = new List<Preference>();

        _mockPreferenceService.Setup(x => x.GetAllPreferencesAsync())
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.GetByUserEmail(email);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);

        _mockPreferenceService.Verify(x => x.GetAllPreferencesAsync(), Times.Once);
    }

    [Fact]
    public async Task GetByUserEmail_WithNullUser_ShouldReturnNotFound()
    {
        // Arrange
        var email = "test@example.com";
        var preferences = new List<Preference>
        {
            new Preference { Id = 1, UserId = 1, WcagVersion = "2.1", WcagLevel = WcagLevel.AA, Language = Language.es, VisualTheme = VisualTheme.light, ReportFormat = ReportFormat.pdf, NotificationsEnabled = true, AiResponseLevel = AiResponseLevel.intermediate, FontSize = 14, User = null!, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }
        };

        _mockPreferenceService.Setup(x => x.GetAllPreferencesAsync())
            .ReturnsAsync(preferences);

        // Act
        var result = await _controller.GetByUserEmail(email);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);

        _mockPreferenceService.Verify(x => x.GetAllPreferencesAsync(), Times.Once);
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_WithValidDto_ShouldReturnCreatedResult()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        var createdPreference = new Preference
        {
            Id = 1,
            UserId = 1,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.es,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockPreferenceService.Setup(x => x.CreatePreferenceAsync(It.IsAny<Preference>()))
            .ReturnsAsync(createdPreference);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<CreatedResult>();
        var createdResult = result as CreatedResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Location.Should().Be("/api/preference/1");

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithInvalidWcagVersion_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "3.0", // Invalid version
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithInvalidWcagLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AAAA", // Invalid level
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithInvalidLanguage_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: "fr", // Invalid language
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithInvalidVisualTheme_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "blue", // Invalid theme
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithInvalidReportFormat_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "xml", // Invalid format
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithInvalidAiResponseLevel_ShouldReturnBadRequest()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "expert", // Invalid level
            FontSize: 14
        );

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
        var badRequestResult = result as BadRequestObjectResult;
        badRequestResult!.StatusCode.Should().Be(400);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Never);
    }

    [Fact]
    public async Task Create_WithNullOptionalFields_ShouldUseDefaults()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: null, // Should default to es
            VisualTheme: null, // Should default to light
            ReportFormat: null, // Should default to pdf
            NotificationsEnabled: null, // Should default to true
            AiResponseLevel: null, // Should default to intermediate
            FontSize: null // Should default to 14
        );

        var createdPreference = new Preference
        {
            Id = 1,
            UserId = 1,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.es,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockPreferenceService.Setup(x => x.CreatePreferenceAsync(It.IsAny<Preference>()))
            .ReturnsAsync(createdPreference);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<CreatedResult>();
        var createdResult = result as CreatedResult;
        createdResult!.StatusCode.Should().Be(201);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Once);
    }

    [Fact]
    public async Task Create_WithConflict_ShouldReturnConflict()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        _mockPreferenceService.Setup(x => x.CreatePreferenceAsync(It.IsAny<Preference>()))
            .ThrowsAsync(new InvalidOperationException("Preferences already exist"));

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult!.StatusCode.Should().Be(409);

        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Once);
    }

    [Theory]
    [InlineData("2.0")]
    [InlineData("2.1")]
    [InlineData("2.2")]
    public async Task Create_WithValidWcagVersions_ShouldReturnCreated(string wcagVersion)
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 1,
            WcagVersion: wcagVersion,
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 14
        );

        var createdPreference = new Preference
        {
            Id = 1,
            UserId = 1,
            WcagVersion = wcagVersion,
            WcagLevel = WcagLevel.AA,
            Language = Language.es,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockPreferenceService.Setup(x => x.CreatePreferenceAsync(It.IsAny<Preference>()))
            .ReturnsAsync(createdPreference);

        // Act
        var result = await _controller.Create(dto);

        // Assert
        result.Should().BeOfType<CreatedResult>();
        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.IsAny<Preference>()), Times.Once);
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_WithExistingPreference_ShouldReturnOk()
    {
        // Arrange
        var preferenceId = 1;

        _mockPreferenceService.Setup(x => x.DeletePreferenceAsync(preferenceId))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.Delete(preferenceId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);
        okResult.Value.Should().NotBeNull();

        _mockPreferenceService.Verify(x => x.DeletePreferenceAsync(preferenceId), Times.Once);
    }

    [Fact]
    public async Task Delete_WithNonExistentPreference_ShouldReturnNotFound()
    {
        // Arrange
        var preferenceId = 999;

        _mockPreferenceService.Setup(x => x.DeletePreferenceAsync(preferenceId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(preferenceId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);

        _mockPreferenceService.Verify(x => x.DeletePreferenceAsync(preferenceId), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MaxValue)]
    public async Task Delete_WithVariousIds_ShouldCallServiceCorrectly(int preferenceId)
    {
        // Arrange
        _mockPreferenceService.Setup(x => x.DeletePreferenceAsync(preferenceId))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.Delete(preferenceId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        _mockPreferenceService.Verify(x => x.DeletePreferenceAsync(preferenceId), Times.Once);
    }

    #endregion

    #region Parameter Validation Tests

    [Fact]
    public async Task GetByUserEmail_ShouldCallServiceWithoutParameters()
    {
        // Arrange
        var email = "test@example.com";
        var preferences = new List<Preference>();

        _mockPreferenceService.Setup(x => x.GetAllPreferencesAsync())
            .ReturnsAsync(preferences);

        // Act
        await _controller.GetByUserEmail(email);

        // Assert
        _mockPreferenceService.Verify(x => x.GetAllPreferencesAsync(), Times.Once);
        _mockPreferenceService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldCallServiceWithCorrectEntity()
    {
        // Arrange
        var dto = new PreferenceCreateDto(
            UserId: 42,
            WcagVersion: "2.1",
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: "pdf",
            NotificationsEnabled: true,
            AiResponseLevel: "intermediate",
            FontSize: 16
        );

        var createdPreference = new Preference
        {
            Id = 1,
            UserId = 42,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.es,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate,
            FontSize = 16,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _mockPreferenceService.Setup(x => x.CreatePreferenceAsync(It.IsAny<Preference>()))
            .ReturnsAsync(createdPreference);

        // Act
        await _controller.Create(dto);

        // Assert
        _mockPreferenceService.Verify(x => x.CreatePreferenceAsync(It.Is<Preference>(p =>
            p.UserId == 42 &&
            p.WcagVersion == "2.1" &&
            p.WcagLevel == WcagLevel.AA &&
            p.FontSize == 16
        )), Times.Once);
    }

    [Fact]
    public async Task Delete_ShouldCallServiceWithCorrectParameter()
    {
        // Arrange
        var preferenceId = 42;

        _mockPreferenceService.Setup(x => x.DeletePreferenceAsync(preferenceId))
            .ReturnsAsync(true);

        // Act
        await _controller.Delete(preferenceId);

        // Assert
        _mockPreferenceService.Verify(x => x.DeletePreferenceAsync(42), Times.Once);
        _mockPreferenceService.VerifyNoOtherCalls();
    }

    #endregion
}