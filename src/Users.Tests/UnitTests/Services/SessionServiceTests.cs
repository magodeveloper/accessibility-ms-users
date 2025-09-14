using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Users.Application.Dtos;
using Users.Application.Services.Session;
using Users.Domain.Entities;
using Users.Infrastructure.Data;
using Xunit;

namespace Users.Tests.UnitTests.Services;

public class SessionServiceTests : IDisposable
{
    private readonly UsersDbContext _context;
    private readonly SessionService _sessionService;
    private bool _disposed;

    public SessionServiceTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UsersDbContext(options);
        _sessionService = new SessionService(_context);
    }

    [Fact]
    public async Task GetSessionsByUserIdAsync_ShouldReturnSessionsForUser()
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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session1 = new Session
        {
            UserId = user.Id,
            TokenHash = "token1_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = user
        };

        var session2 = new Session
        {
            UserId = user.Id,
            TokenHash = "token2_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            User = user
        };

        _context.Sessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.GetSessionsByUserIdAsync(user.Id);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.UserId == user.Id);
        result.Should().OnlyContain(s => s.User != null && s.User.Email == "test@example.com");
    }

    [Fact]
    public async Task GetSessionsByUserIdAsync_ShouldReturnEmptyWhenNoSessions()
    {
        // Arrange
        var nonExistentUserId = 999;

        // Act
        var result = await _sessionService.GetSessionsByUserIdAsync(nonExistentUserId);

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateSessionAsync_ShouldCreateAndReturnSession()
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
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = "new_token_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = await _sessionService.CreateSessionAsync(session);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().BeGreaterThan(0);
        result.TokenHash.Should().Be("new_token_hash");
        result.UserId.Should().Be(user.Id);

        // Verify it was saved to database
        var savedSession = await _context.Sessions.FindAsync(result.Id);
        savedSession.Should().NotBeNull();
        savedSession!.TokenHash.Should().Be("new_token_hash");
    }

    [Fact]
    public async Task GetSessionByIdAsync_ShouldReturnSessionWhenExists()
    {
        // Arrange
        var user = new User
        {
            Nickname = "getuser",
            Name = "Get",
            Lastname = "User",
            Email = "get@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = "get_token_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = user
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.GetSessionByIdAsync(session.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
        result.TokenHash.Should().Be("get_token_hash");
        result.UserId.Should().Be(user.Id);
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be("get@example.com");
    }

    [Fact]
    public async Task GetSessionByIdAsync_ShouldReturnNullWhenNotExists()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _sessionService.GetSessionByIdAsync(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAllSessionsAsync_ShouldReturnAllSessions()
    {
        // Arrange
        var user1 = new User
        {
            Nickname = "user1",
            Name = "User",
            Lastname = "One",
            Email = "user1@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var user2 = new User
        {
            Nickname = "user2",
            Name = "User",
            Lastname = "Two",
            Email = "user2@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.AddRange(user1, user2);
        await _context.SaveChangesAsync();

        var session1 = new Session
        {
            UserId = user1.Id,
            TokenHash = "token1_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = user1
        };

        var session2 = new Session
        {
            UserId = user2.Id,
            TokenHash = "token2_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2),
            User = user2
        };

        _context.Sessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.GetAllSessionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().OnlyContain(s => s.User != null);
        result.Select(s => s.User!.Email).Should().Contain(new[] { "user1@example.com", "user2@example.com" });
    }

    [Fact]
    public async Task GetAllSessionsAsync_ShouldReturnEmptyWhenNoSessions()
    {
        // Act
        var result = await _sessionService.GetAllSessionsAsync();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateSessionAsync_ShouldUpdateExistingSession()
    {
        // Arrange
        var user = new User
        {
            Nickname = "updateuser",
            Name = "Update",
            Lastname = "User",
            Email = "update@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = "update_token_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        var newExpiresAt = DateTime.UtcNow.AddHours(5);
        session.ExpiresAt = newExpiresAt;

        // Act
        var result = await _sessionService.UpdateSessionAsync(session);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(session.Id);
        result.ExpiresAt.Should().Be(newExpiresAt);

        // Verify it was updated in database
        var updatedSession = await _context.Sessions.FindAsync(session.Id);
        updatedSession.Should().NotBeNull();
        updatedSession!.ExpiresAt.Should().Be(newExpiresAt);
    }

    [Fact]
    public async Task UpdateSessionAsync_ShouldReturnNullWhenSessionNotExists()
    {
        // Arrange
        var nonExistentSession = new Session
        {
            Id = 999,
            UserId = 1,
            TokenHash = "non_existent_token",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = await _sessionService.UpdateSessionAsync(nonExistentSession);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldDeleteExistingSession()
    {
        // Arrange
        var user = new User
        {
            Nickname = "deleteuser",
            Name = "Delete",
            Lastname = "User",
            Email = "delete@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session = new Session
        {
            UserId = user.Id,
            TokenHash = "delete_token_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.DeleteSessionAsync(session.Id);

        // Assert
        result.Should().BeTrue();

        // Verify it was deleted from database
        var deletedSession = await _context.Sessions.FindAsync(session.Id);
        deletedSession.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSessionAsync_ShouldReturnFalseWhenSessionNotExists()
    {
        // Arrange
        var nonExistentId = 999;

        // Act
        var result = await _sessionService.DeleteSessionAsync(nonExistentId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSessionsByUserIdAsync_ShouldDeleteAllUserSessions()
    {
        // Arrange
        var user = new User
        {
            Nickname = "deleteuseruser",
            Name = "DeleteUser",
            Lastname = "User",
            Email = "deleteuser@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var session1 = new Session
        {
            UserId = user.Id,
            TokenHash = "delete_token1_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(1)
        };

        var session2 = new Session
        {
            UserId = user.Id,
            TokenHash = "delete_token2_hash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };

        _context.Sessions.AddRange(session1, session2);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sessionService.DeleteSessionsByUserIdAsync(user.Id);

        // Assert
        result.Should().BeTrue();

        // Verify they were deleted from database
        var remainingSessions = await _context.Sessions.Where(s => s.UserId == user.Id).ToListAsync();
        remainingSessions.Should().BeEmpty();
    }

    [Fact]
    public async Task DeleteSessionsByUserIdAsync_ShouldReturnFalseWhenNoSessionsToDelete()
    {
        // Arrange
        var nonExistentUserId = 999;

        // Act
        var result = await _sessionService.DeleteSessionsByUserIdAsync(nonExistentUserId);

        // Assert
        result.Should().BeFalse();
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