using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Users.Application.Services.Preference;
using Users.Domain.Entities;
using Users.Infrastructure.Data;
using Xunit;

namespace Users.Tests.UnitTests.Services;

public class PreferenceServiceTests : IDisposable
{
    private readonly UsersDbContext _context;
    private readonly PreferenceService _preferenceService;
    private bool _disposed;

    public PreferenceServiceTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UsersDbContext(options);
        _preferenceService = new PreferenceService(_context);
    }

    [Fact]
    public async Task CreatePreferenceAsync_ShouldCreatePreferenceSuccessfully()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var preference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 14
        };

        // Act
        var result = await _preferenceService.CreatePreferenceAsync(preference);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.UserId.Should().Be(user.Id);
        result.WcagVersion.Should().Be("2.1");
        result.WcagLevel.Should().Be(WcagLevel.AA);
        result.Language.Should().Be(Language.en);
        result.VisualTheme.Should().Be(VisualTheme.dark);
        result.ReportFormat.Should().Be(ReportFormat.html);
        result.NotificationsEnabled.Should().BeTrue();
        result.AiResponseLevel.Should().Be(AiResponseLevel.detailed);
        result.FontSize.Should().Be(14);
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        // Verify it was saved to database
        var savedPreference = await _context.Preferences.FindAsync(result.Id);
        savedPreference.Should().NotBeNull();
        savedPreference!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreatePreferenceAsync_WhenPreferenceAlreadyExists_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var existingPreference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = false,
            AiResponseLevel = AiResponseLevel.basic,
            FontSize = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _context.Preferences.AddAsync(existingPreference);
        await _context.SaveChangesAsync();

        var newPreference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.2",
            WcagLevel = WcagLevel.AAA,
            Language = Language.es,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 16
        };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _preferenceService.CreatePreferenceAsync(newPreference));

        exception.Message.Should().NotBeEmpty();
    }

    [Fact]
    public async Task GetPreferenceByIdAsync_WhenPreferenceExists_ShouldReturnPreferenceWithUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };
        await _context.Users.AddAsync(user);

        var preference = new Preference
        {
            Id = 1,
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = user
        };
        await _context.Preferences.AddAsync(preference);
        await _context.SaveChangesAsync();

        // Act
        var result = await _preferenceService.GetPreferenceByIdAsync(1);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.UserId.Should().Be(user.Id);
        result.WcagVersion.Should().Be("2.1");
        result.WcagLevel.Should().Be(WcagLevel.AA);
        result.Language.Should().Be(Language.en);
        result.VisualTheme.Should().Be(VisualTheme.dark);
        result.ReportFormat.Should().Be(ReportFormat.html);
        result.NotificationsEnabled.Should().BeTrue();
        result.AiResponseLevel.Should().Be(AiResponseLevel.detailed);
        result.FontSize.Should().Be(14);
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task GetPreferenceByIdAsync_WhenPreferenceDoesNotExist_ShouldReturnNull()
    {
        // Act
        var result = await _preferenceService.GetPreferenceByIdAsync(999);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllPreferencesAsync_WhenPreferencesExist_ShouldReturnAllPreferencesWithUsers()
    {
        // Arrange
        var user1 = new User
        {
            Id = 1,
            Nickname = "user1",
            Name = "User",
            Lastname = "One",
            Email = "user1@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };

        var user2 = new User
        {
            Id = 2,
            Nickname = "user2",
            Name = "User",
            Lastname = "Two",
            Email = "user2@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };

        await _context.Users.AddRangeAsync(user1, user2);

        var preference1 = new Preference
        {
            Id = 1,
            UserId = user1.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = user1
        };

        var preference2 = new Preference
        {
            Id = 2,
            UserId = user2.Id,
            WcagVersion = "2.2",
            WcagLevel = WcagLevel.AAA,
            Language = Language.es,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = false,
            AiResponseLevel = AiResponseLevel.basic,
            FontSize = 12,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = user2
        };

        await _context.Preferences.AddRangeAsync(preference1, preference2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _preferenceService.GetAllPreferencesAsync();

        // Assert
        var preferences = result.ToList();
        preferences.Should().HaveCount(2);

        var pref1 = preferences.First(p => p.UserId == user1.Id);
        pref1.WcagVersion.Should().Be("2.1");
        pref1.Language.Should().Be(Language.en);
        pref1.User.Should().NotBeNull();
        pref1.User!.Email.Should().Be("user1@example.com");

        var pref2 = preferences.First(p => p.UserId == user2.Id);
        pref2.WcagVersion.Should().Be("2.2");
        pref2.Language.Should().Be(Language.es);
        pref2.User.Should().NotBeNull();
        pref2.User!.Email.Should().Be("user2@example.com");
    }

    [Fact]
    public async Task GetAllPreferencesAsync_WhenNoPreferencesExist_ShouldReturnEmptyList()
    {
        // Act
        var result = await _preferenceService.GetAllPreferencesAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdatePreferenceAsync_WhenPreferenceExists_ShouldUpdateAndReturnPreference()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };
        await _context.Users.AddAsync(user);

        var originalPreference = new Preference
        {
            Id = 1,
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = false,
            AiResponseLevel = AiResponseLevel.basic,
            FontSize = 12,
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            User = user
        };
        await _context.Preferences.AddAsync(originalPreference);
        await _context.SaveChangesAsync();

        var updateData = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.2",
            WcagLevel = WcagLevel.AAA,
            Language = Language.es,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 16
        };

        // Act
        var result = await _preferenceService.UpdatePreferenceAsync(updateData);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.UserId.Should().Be(user.Id);
        result.WcagVersion.Should().Be("2.2");
        result.WcagLevel.Should().Be(WcagLevel.AAA);
        result.Language.Should().Be(Language.es);
        result.VisualTheme.Should().Be(VisualTheme.dark);
        result.ReportFormat.Should().Be(ReportFormat.html);
        result.NotificationsEnabled.Should().BeTrue();
        result.AiResponseLevel.Should().Be(AiResponseLevel.detailed);
        result.FontSize.Should().Be(16);
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(-1), TimeSpan.FromSeconds(5));

        // Verify it was updated in database
        var savedPreference = await _context.Preferences.FindAsync(1);
        savedPreference.Should().NotBeNull();
        savedPreference!.WcagVersion.Should().Be("2.2");
        savedPreference.Language.Should().Be(Language.es);
        savedPreference.VisualTheme.Should().Be(VisualTheme.dark);
    }

    [Fact]
    public async Task UpdatePreferenceAsync_WhenPreferenceDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var updateData = new Preference
        {
            UserId = 999,
            WcagVersion = "2.2",
            WcagLevel = WcagLevel.AAA,
            Language = Language.es,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 16
        };

        // Act
        var result = await _preferenceService.UpdatePreferenceAsync(updateData);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeletePreferenceAsync_WhenPreferenceExists_ShouldDeleteAndReturnTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };
        await _context.Users.AddAsync(user);

        var preference = new Preference
        {
            Id = 1,
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = true,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 14,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            User = user
        };
        await _context.Preferences.AddAsync(preference);
        await _context.SaveChangesAsync();

        // Act
        var result = await _preferenceService.DeletePreferenceAsync(1);

        // Assert
        result.Should().BeTrue();

        // Verify it was deleted from database
        var deletedPreference = await _context.Preferences.FindAsync(1);
        deletedPreference.Should().BeNull();
    }

    [Fact]
    public async Task DeletePreferenceAsync_WhenPreferenceDoesNotExist_ShouldReturnFalse()
    {
        // Act
        var result = await _preferenceService.DeletePreferenceAsync(999);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UpdatePreferenceAsync_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Status = UserStatus.active,
            EmailConfirmed = true
        };
        await _context.Users.AddAsync(user);

        var originalDate = DateTime.UtcNow.AddDays(-1);
        var originalPreference = new Preference
        {
            Id = 1,
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.light,
            ReportFormat = ReportFormat.pdf,
            NotificationsEnabled = false,
            AiResponseLevel = AiResponseLevel.basic,
            FontSize = 12,
            CreatedAt = originalDate,
            UpdatedAt = originalDate,
            User = user
        };
        await _context.Preferences.AddAsync(originalPreference);
        await _context.SaveChangesAsync();

        var updateData = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1", // Same
            WcagLevel = WcagLevel.AAA, // Changed
            Language = Language.en, // Same
            VisualTheme = VisualTheme.dark, // Changed
            ReportFormat = ReportFormat.pdf, // Same
            NotificationsEnabled = false, // Same
            AiResponseLevel = AiResponseLevel.detailed, // Changed
            FontSize = 12 // Same
        };

        // Act
        var result = await _preferenceService.UpdatePreferenceAsync(updateData);

        // Assert
        result.Should().NotBeNull();
        result!.WcagVersion.Should().Be("2.1"); // Unchanged
        result.WcagLevel.Should().Be(WcagLevel.AAA); // Changed
        result.Language.Should().Be(Language.en); // Unchanged
        result.VisualTheme.Should().Be(VisualTheme.dark); // Changed
        result.ReportFormat.Should().Be(ReportFormat.pdf); // Unchanged
        result.NotificationsEnabled.Should().BeFalse(); // Unchanged
        result.AiResponseLevel.Should().Be(AiResponseLevel.detailed); // Changed
        result.FontSize.Should().Be(12); // Unchanged
        result.CreatedAt.Should().BeCloseTo(originalDate, TimeSpan.FromSeconds(1)); // Should not change
        result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5)); // Should be updated
    }
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}