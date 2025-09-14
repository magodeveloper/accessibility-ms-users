using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Infrastructure.Data;
using Xunit;
using FluentAssertions;

namespace Users.Tests.UnitTests.Infrastructure;

public class UsersDbContextTests : IDisposable
{
    private readonly UsersDbContext _context;
    private bool _disposed;

    public UsersDbContextTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
            .Options;
        _context = new UsersDbContext(options);
    }

    [Fact]
    public void DbContext_ShouldCreateUsersDbSet()
    {
        // Assert
        _context.Users.Should().NotBeNull();
        _context.Sessions.Should().NotBeNull();
        _context.Preferences.Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_ShouldSaveAndRetrieveUser()
    {
        // Arrange
        var user = new User
        {
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");

        // Assert
        savedUser.Should().NotBeNull();
        savedUser!.Nickname.Should().Be("testuser");
        savedUser.Name.Should().Be("Test");
        savedUser.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task DbContext_ShouldSaveAndRetrieveSession()
    {
        // Arrange
        var user = new User
        {
            Nickname = "sessionuser",
            Name = "Session",
            Lastname = "User",
            Email = "session@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = "tokenhash123",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var savedSession = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.TokenHash == "tokenhash123");

        // Assert
        savedSession.Should().NotBeNull();
        savedSession!.UserId.Should().Be(user.Id);
        savedSession.User.Should().NotBeNull();
        savedSession.User!.Email.Should().Be("session@example.com");
    }

    [Fact]
    public async Task DbContext_ShouldSaveAndRetrievePreference()
    {
        // Arrange
        var user = new User
        {
            Nickname = "prefuser",
            Name = "Pref",
            Lastname = "User",
            Email = "pref@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.2",
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

        // Act
        _context.Preferences.Add(preference);
        await _context.SaveChangesAsync();

        var savedPreference = await _context.Preferences
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == user.Id);

        // Assert
        savedPreference.Should().NotBeNull();
        savedPreference!.WcagVersion.Should().Be("2.2");
        savedPreference.WcagLevel.Should().Be(WcagLevel.AA);
        savedPreference.User.Should().NotBeNull();
        savedPreference.User!.Email.Should().Be("pref@example.com");
    }

    [Fact]
    public void DbContext_ShouldHaveCorrectTableNames()
    {
        // Act
        var userEntityType = _context.Model.FindEntityType(typeof(User));
        var sessionEntityType = _context.Model.FindEntityType(typeof(Session));
        var preferenceEntityType = _context.Model.FindEntityType(typeof(Preference));

        // Assert
        userEntityType!.GetTableName().Should().Be("users");
        sessionEntityType!.GetTableName().Should().Be("SESSIONS");
        preferenceEntityType!.GetTableName().Should().Be("PREFERENCES");
    }

    [Fact]
    public async Task DbContext_ShouldCascadeDeleteUserSessions()
    {
        // Arrange
        var user = new User
        {
            Nickname = "cascadeuser",
            Name = "Cascade",
            Lastname = "User",
            Email = "cascade@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = "cascadetoken",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act - Delete user
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Assert - Session should also be deleted
        var remainingSession = await _context.Sessions.FirstOrDefaultAsync(s => s.TokenHash == "cascadetoken");
        remainingSession.Should().BeNull();
    }
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
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
}