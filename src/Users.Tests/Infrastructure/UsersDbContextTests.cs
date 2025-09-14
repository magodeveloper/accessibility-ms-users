using Microsoft.EntityFrameworkCore;
using Users.Domain.Entities;
using Users.Infrastructure.Data;

namespace Users.Tests.Infrastructure;

public class UsersDbContextTests : IDisposable
{
    private readonly UsersDbContext _context;
    private bool _disposed;

    public UsersDbContextTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UsersDbContext(options);
    }

    [Fact]
    public void DbContext_ShouldHaveCorrectDbSets()
    {
        // Assert
        Assert.NotNull(_context.Users);
        Assert.NotNull(_context.Preferences);
        Assert.NotNull(_context.Sessions);
    }

    [Fact]
    public async Task DbContext_CanSaveAndRetrieveUser()
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
            EmailConfirmed = true,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == "test@example.com");
        Assert.NotNull(retrievedUser);
        Assert.Equal("testuser", retrievedUser.Nickname);
    }

    [Fact]
    public async Task DbContext_CanSaveAndRetrievePreference()
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
            EmailConfirmed = true,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            FontSize = 16,
            NotificationsEnabled = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // Act
        _context.Preferences.Add(preference);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedPreference = await _context.Preferences
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.UserId == user.Id);
        Assert.NotNull(retrievedPreference);
        Assert.Equal(VisualTheme.dark, retrievedPreference.VisualTheme);
        Assert.NotNull(retrievedPreference.User);
    }

    [Fact]
    public async Task DbContext_CanSaveAndRetrieveSession()
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
            EmailConfirmed = true,
            RegistrationDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = "test-token-hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Assert
        var retrievedSession = await _context.Sessions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == user.Id);
        Assert.NotNull(retrievedSession);
        Assert.Equal("test-token-hash", retrievedSession.TokenHash);
        Assert.NotNull(retrievedSession.User);
    }

    [Fact]
    public void DbContext_HasCorrectModelConfiguration()
    {
        // Act & Assert
        var userEntityType = _context.Model.FindEntityType(typeof(User));
        var preferenceEntityType = _context.Model.FindEntityType(typeof(Preference));
        var sessionEntityType = _context.Model.FindEntityType(typeof(Session));

        Assert.NotNull(userEntityType);
        Assert.NotNull(preferenceEntityType);
        Assert.NotNull(sessionEntityType);

        // Check if primary keys are configured
        Assert.NotNull(userEntityType.FindPrimaryKey());
        Assert.NotNull(preferenceEntityType.FindPrimaryKey());
        Assert.NotNull(sessionEntityType.FindPrimaryKey());
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context?.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}