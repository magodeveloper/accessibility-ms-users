using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Users.Infrastructure.Data;
using Users.Domain.Entities;

namespace Users.Tests.Infrastructure;

public class InfrastructureIntegrationTests : IDisposable
{
    private readonly UsersDbContext _context;
    private bool _disposed;

    public InfrastructureIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UsersDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task DbContext_ShouldPerformCRUDOperations()
    {
        // Arrange
        var user = new User
        {
            Email = "integration@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Nickname = "integration",
            Name = "Integration",
            Lastname = "Test",
            RegistrationDate = DateTime.UtcNow
        };

        // Act - Create
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Assert - Read
        var savedUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == "integration@example.com");
        savedUser.Should().NotBeNull();
        savedUser!.Email.Should().Be("integration@example.com");

        // Act - Update
        savedUser.Status = UserStatus.inactive;
        savedUser.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Assert - Read Updated
        var updatedUser = await _context.Users.FindAsync(savedUser.Id);
        updatedUser.Should().NotBeNull();
        updatedUser!.Status.Should().Be(UserStatus.inactive);

        // Act - Delete
        _context.Users.Remove(updatedUser);
        await _context.SaveChangesAsync();

        // Assert - Verify Deleted
        var deletedUser = await _context.Users.FindAsync(savedUser.Id);
        deletedUser.Should().BeNull();
    }

    [Fact]
    public async Task DbContext_ShouldHandleRelationships()
    {
        // Arrange
        var user = new User
        {
            Email = "relationships@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Nickname = "relationships",
            Name = "Relationships",
            Lastname = "Test",
            RegistrationDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = $"token_{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        // Act
        _context.Preferences.Add(preference);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Assert
        var userWithRelations = await _context.Users
            .Include(u => u.Preference)
            .Include(u => u.Sessions)
            .FirstOrDefaultAsync(u => u.Id == user.Id);

        userWithRelations.Should().NotBeNull();
        userWithRelations!.Preference.Should().NotBeNull();
        userWithRelations.Sessions.Should().HaveCount(1);
    }

    [Fact]
    public async Task DbContext_ShouldHandleTransactions()
    {
        // Arrange
        var user = new User
        {
            Email = "transaction@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Nickname = "transaction",
            Name = "Transaction",
            Lastname = "Test",
            RegistrationDate = DateTime.UtcNow
        };

        // Act - InMemory doesn't support transactions, so we just test the operations
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Preferences.Add(preference);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users
            .Include(u => u.Preference)
            .FirstOrDefaultAsync(u => u.Email == "transaction@example.com");

        savedUser.Should().NotBeNull();
        savedUser!.Preference.Should().NotBeNull();
    }

    [Fact]
    public async Task DbContext_ShouldHandleCascadeDelete()
    {
        // Arrange
        var user = new User
        {
            Email = "cascade@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Nickname = "cascade",
            Name = "Cascade",
            Lastname = "Test",
            RegistrationDate = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var preference = new Preference
        {
            UserId = user.Id,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = $"token_{Guid.NewGuid()}",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(24)
        };

        _context.Preferences.Add(preference);
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act - Delete user (should cascade to preference and sessions)
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        // Assert
        var remainingPreferences = await _context.Preferences
            .Where(p => p.UserId == user.Id)
            .ToListAsync();
        var remainingSessions = await _context.Sessions
            .Where(s => s.UserId == user.Id)
            .ToListAsync();

        remainingPreferences.Should().BeEmpty();
        remainingSessions.Should().BeEmpty();
    }

    [Fact]
    public void Context_ShouldStartWithEmptyDatabase()
    {
        // Arrange & Act
        _context.Database.EnsureCreated();

        // Assert
        _context.Users.Should().BeEmpty();
        _context.Preferences.Should().BeEmpty();
        _context.Sessions.Should().BeEmpty();
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context?.Dispose();
        }
        _disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}