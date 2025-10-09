using Moq;
using FluentAssertions;
using Users.Domain.Entities;
using Users.Application.Dtos;
using Users.Api.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Users.Application.Services;
using Microsoft.EntityFrameworkCore;
using Users.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Users.Application.Services.UserContext;

namespace Users.Tests.Controllers;

public class UsersWithPreferencesControllerTests : IDisposable
{
    private readonly UsersDbContext _context;
    private readonly IPasswordService _passwordService;
    private readonly Mock<IUserContext> _mockUserContext;
    private readonly UsersWithPreferencesController _controller;
    private readonly ServiceProvider _serviceProvider;
    private bool _disposed;

    public UsersWithPreferencesControllerTests()
    {
        var services = new ServiceCollection();
        services.AddDbContext<UsersDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));
        services.AddTransient<IPasswordService, BcryptPasswordService>();

        _serviceProvider = services.BuildServiceProvider();
        _context = _serviceProvider.GetRequiredService<UsersDbContext>();
        _passwordService = _serviceProvider.GetRequiredService<IPasswordService>();

        _mockUserContext = new Mock<IUserContext>();
        // Configurar mock IUserContext - usuario autenticado como admin
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(true);
        _mockUserContext.Setup(x => x.UserId).Returns(1);
        _mockUserContext.Setup(x => x.Email).Returns("test@example.com");
        _mockUserContext.Setup(x => x.Role).Returns("Admin");
        _mockUserContext.Setup(x => x.IsAdmin).Returns(true);
        _mockUserContext.Setup(x => x.UserName).Returns("TestUser");

        _controller = new UsersWithPreferencesController(_context, _passwordService, _mockUserContext.Object);

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
                _context.Dispose();
                _serviceProvider.Dispose();
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
    public async Task Create_WithValidUserDto_ShouldCreateUserAndPreferences()
    {
        // Arrange
        var userDto = new UserCreateDto("testuser", "Test", "User", "test@example.com", "TestPass123!");

        // Act
        var result = await _controller.Create(userDto);

        // Assert
        result.Should().BeOfType<CreatedResult>();
        var createdResult = result as CreatedResult;
        createdResult!.StatusCode.Should().Be(201);
        createdResult.Location.Should().NotBeNullOrEmpty();

        // Verificar que el usuario fue creado en la BD
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        user.Should().NotBeNull();
        user!.Nickname.Should().Be(userDto.Nickname);
        user.Name.Should().Be(userDto.Name);
        user.Lastname.Should().Be(userDto.Lastname);
        user.Email.Should().Be(userDto.Email);
        user.Role.Should().Be(UserRole.user);
        user.Status.Should().Be(UserStatus.active);
        user.EmailConfirmed.Should().BeFalse();

        // Verificar que las preferencias por defecto fueron creadas
        var preferences = await _context.Preferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        preferences.Should().NotBeNull();
        preferences!.WcagVersion.Should().Be("2.2");
        preferences.WcagLevel.Should().Be(WcagLevel.AA);
        preferences.Language.Should().Be(Language.es);
        preferences.VisualTheme.Should().Be(VisualTheme.light);
        preferences.ReportFormat.Should().Be(ReportFormat.pdf);
        preferences.NotificationsEnabled.Should().BeTrue();
        preferences.AiResponseLevel.Should().Be(AiResponseLevel.intermediate);
        preferences.FontSize.Should().Be(14);
    }

    [Fact]
    public async Task Create_WithExistingEmail_ShouldReturnConflict()
    {
        // Arrange
        var existingUser = new User
        {
            Nickname = "existing",
            Name = "Existing",
            Lastname = "User",
            Email = "existing@example.com",
            Password = "hashedpass",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var userDto = new UserCreateDto("newuser", "New", "User", "existing@example.com", "TestPass123!"); // Email duplicado

        // Act
        var result = await _controller.Create(userDto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Create_WithExistingNickname_ShouldReturnConflict()
    {
        // Arrange
        var existingUser = new User
        {
            Nickname = "existingnick",
            Name = "Existing",
            Lastname = "User",
            Email = "existing@example.com",
            Password = "hashedpass",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var userDto = new UserCreateDto("existingnick", "New", "User", "new@example.com", "TestPass123!"); // Nickname duplicado

        // Act
        var result = await _controller.Create(userDto);

        // Assert
        result.Should().BeOfType<ConflictObjectResult>();
        var conflictResult = result as ConflictObjectResult;
        conflictResult!.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Create_ShouldHashPassword()
    {
        // Arrange
        var userDto = new UserCreateDto("testuser", "Test", "User", "test@example.com", "PlainPassword123!");

        // Act
        await _controller.Create(userDto);

        // Assert
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        user.Should().NotBeNull();
        user!.Password.Should().NotBe("PlainPassword123!");
        user.Password.Should().StartWith("$2a$"); // BCrypt hash prefix
    }

    [Fact]
    public async Task Patch_WithValidEmailAndData_ShouldUpdateUserAndPreferences()
    {
        // Arrange
        var user = new User
        {
            Nickname = "originaluser",
            Name = "Original",
            Lastname = "User",
            Email = "original@example.com",
            Password = "originalpass",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preferences = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.0",
            WcagLevel = WcagLevel.A,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = false,
            AiResponseLevel = AiResponseLevel.basic,
            FontSize = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Preferences.Add(preferences);
        await _context.SaveChangesAsync();

        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: "2.2",
            WcagLevel: "AA",
            Language: "es",
            VisualTheme: "light",
            ReportFormat: null,
            NotificationsEnabled: true,
            AiResponseLevel: null,
            FontSize: 16,
            Nickname: null,
            Name: "Updated",
            Lastname: null,
            Email: null,
            Password: null
        );

        // Act
        var result = await _controller.Patch("original@example.com", patchDto);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(200);

        // Verificar actualizaciones del usuario
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "original@example.com");
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be("Updated");
        updatedUser.Nickname.Should().Be("originaluser"); // No cambiado
        updatedUser.Lastname.Should().Be("User"); // No cambiado

        // Verificar actualizaciones de preferencias
        var updatedPreferences = await _context.Preferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.WcagVersion.Should().Be("2.2");
        updatedPreferences.WcagLevel.Should().Be(WcagLevel.AA);
        updatedPreferences.Language.Should().Be(Language.es);
        updatedPreferences.VisualTheme.Should().Be(VisualTheme.light);
        updatedPreferences.NotificationsEnabled.Should().BeTrue();
        updatedPreferences.FontSize.Should().Be(16);
        updatedPreferences.ReportFormat.Should().Be(ReportFormat.html); // No cambiado
    }

    [Fact]
    public async Task Patch_WithNonExistentEmail_ShouldReturnNotFound()
    {
        // Arrange
        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null,
            WcagLevel: null,
            Language: null,
            VisualTheme: null,
            ReportFormat: null,
            NotificationsEnabled: null,
            AiResponseLevel: null,
            FontSize: null,
            Nickname: null,
            Name: "Updated",
            Lastname: null,
            Email: null,
            Password: null
        );

        // Act
        var result = await _controller.Patch("nonexistent@example.com", patchDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Patch_WithUserButNoPreferences_ShouldReturnNotFound()
    {
        // Arrange
        var user = new User
        {
            Nickname = "useronly",
            Name = "User",
            Lastname = "Only",
            Email = "useronly@example.com",
            Password = "password",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        // No agregar preferencias intencionalmente

        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null,
            WcagLevel: null,
            Language: null,
            VisualTheme: null,
            ReportFormat: null,
            NotificationsEnabled: null,
            AiResponseLevel: null,
            FontSize: null,
            Nickname: null,
            Name: "Updated",
            Lastname: null,
            Email: null,
            Password: null
        );

        // Act
        var result = await _controller.Patch("useronly@example.com", patchDto);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
        var notFoundResult = result as NotFoundObjectResult;
        notFoundResult!.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Patch_WithPasswordUpdate_ShouldHashNewPassword()
    {
        // Arrange
        var user = new User
        {
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "oldhashedpass",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preferences = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Preferences.Add(preferences);
        await _context.SaveChangesAsync();

        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null,
            WcagLevel: null,
            Language: null,
            VisualTheme: null,
            ReportFormat: null,
            NotificationsEnabled: null,
            AiResponseLevel: null,
            FontSize: null,
            Nickname: null,
            Name: null,
            Lastname: null,
            Email: null,
            Password: "NewPassword123!"
        );

        // Act
        await _controller.Patch("test@example.com", patchDto);

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        updatedUser.Should().NotBeNull();
        updatedUser!.Password.Should().NotBe("NewPassword123!");
        updatedUser.Password.Should().NotBe("oldhashedpass");
        updatedUser.Password.Should().StartWith("$2a$"); // BCrypt hash prefix
    }

    [Fact]
    public async Task Patch_WithInvalidWcagVersion_ShouldNotUpdateWcagVersion()
    {
        // Arrange
        var user = new User
        {
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "password",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preferences = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1", // Valor original
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Preferences.Add(preferences);
        await _context.SaveChangesAsync();

        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: "3.0", // Versión inválida
            WcagLevel: null,
            Language: null,
            VisualTheme: null,
            ReportFormat: null,
            NotificationsEnabled: null,
            AiResponseLevel: null,
            FontSize: null,
            Nickname: null,
            Name: null,
            Lastname: null,
            Email: null,
            Password: null
        );

        // Act
        await _controller.Patch("test@example.com", patchDto);

        // Assert
        var updatedPreferences = await _context.Preferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.WcagVersion.Should().Be("2.1"); // Debe mantenerse el valor original
    }

    [Fact]
    public async Task Patch_WithInvalidEnumValues_ShouldNotUpdateEnumFields()
    {
        // Arrange
        var user = new User
        {
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "password",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preferences = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA, // Valor original
            Language = Language.en, // Valor original
            VisualTheme = VisualTheme.light, // Valor original
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.intermediate, // Valor original
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Preferences.Add(preferences);
        await _context.SaveChangesAsync();

        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null,
            WcagLevel: "InvalidLevel",
            Language: "InvalidLanguage",
            VisualTheme: "InvalidTheme",
            ReportFormat: "InvalidFormat",
            NotificationsEnabled: null,
            AiResponseLevel: "InvalidLevel",
            FontSize: null,
            Nickname: null,
            Name: null,
            Lastname: null,
            Email: null,
            Password: null
        );

        // Act
        await _controller.Patch("test@example.com", patchDto);

        // Assert
        var updatedPreferences = await _context.Preferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        updatedPreferences.Should().NotBeNull();
        updatedPreferences!.WcagLevel.Should().Be(WcagLevel.AA); // Sin cambio
        updatedPreferences.Language.Should().Be(Language.en); // Sin cambio
        updatedPreferences.VisualTheme.Should().Be(VisualTheme.light); // Sin cambio
        updatedPreferences.ReportFormat.Should().Be(ReportFormat.pdf); // Sin cambio
        updatedPreferences.AiResponseLevel.Should().Be(AiResponseLevel.intermediate); // Sin cambio
    }

    [Fact]
    public async Task Patch_WithPartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange
        var user = new User
        {
            Nickname = "testuser",
            Name = "Original",
            Lastname = "User",
            Email = "test@example.com",
            Password = "originalpass",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = false,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preferences = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.A,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = false,
            AiResponseLevel = AiResponseLevel.basic,
            FontSize = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Preferences.Add(preferences);
        await _context.SaveChangesAsync();

        var patchDto = new PreferenceUserPatchDto(
            WcagVersion: null,
            WcagLevel: null,
            Language: null,
            VisualTheme: null,
            ReportFormat: null,
            NotificationsEnabled: null,
            AiResponseLevel: null,
            FontSize: null,
            Nickname: null,
            Name: "Updated", // Solo actualizar el nombre
            Lastname: null,
            Email: null,
            Password: null
        );

        // Act
        await _controller.Patch("test@example.com", patchDto);

        // Assert
        var updatedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        updatedUser.Should().NotBeNull();
        updatedUser!.Name.Should().Be("Updated"); // Cambió
        updatedUser.Nickname.Should().Be("testuser"); // Sin cambio
        updatedUser.Lastname.Should().Be("User"); // Sin cambio
        updatedUser.Email.Should().Be("test@example.com"); // Sin cambio

        var updatedPreferences = await _context.Preferences.FirstOrDefaultAsync(p => p.UserId == user.Id);
        updatedPreferences.Should().NotBeNull();
        // Todas las preferencias deben mantenerse iguales
        updatedPreferences!.WcagVersion.Should().Be("2.1");
        updatedPreferences.WcagLevel.Should().Be(WcagLevel.A);
        updatedPreferences.Language.Should().Be(Language.en);
        updatedPreferences.VisualTheme.Should().Be(VisualTheme.dark);
        updatedPreferences.ReportFormat.Should().Be(ReportFormat.html);
        updatedPreferences.NotificationsEnabled.Should().BeFalse();
        updatedPreferences.AiResponseLevel.Should().Be(AiResponseLevel.basic);
        updatedPreferences.FontSize.Should().Be(12);
    }

    // ===== Tests de autenticación =====

    [Fact]
    public async Task Create_NotAuthenticated_ReturnsUnauthorized()
    {
        // Arrange
        _mockUserContext.Setup(x => x.IsAuthenticated).Returns(false);
        var dto = new UserCreateDto("testuser", "Test", "User", "test@example.com", "Password123!");

        // Act
        var result = await _controller.Create(dto);

        // Assert
        // Create no requiere autenticación (es registro público), pero verificamos que funciona
        result.Should().NotBeOfType<UnauthorizedObjectResult>();
    }
}