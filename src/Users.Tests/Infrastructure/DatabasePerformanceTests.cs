using Microsoft.EntityFrameworkCore;
using FluentAssertions;
using Users.Infrastructure.Data;
using Users.Domain.Entities;
using System.Diagnostics;

namespace Users.Tests.Infrastructure;

public class DatabasePerformanceTests : IDisposable
{
    private readonly UsersDbContext _context;
    private bool _disposed;

    public DatabasePerformanceTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        _context = new UsersDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Context_ShouldHandleLargeUserDatasets()
    {
        // Arrange
        var users = new List<User>();
        var baseTime = DateTime.UtcNow;

        for (int i = 0; i < 100; i++)
        {
            users.Add(new User
            {
                Email = $"user{i}@example.com",
                Password = $"hash{i}",
                Role = (i % 2 == 0) ? UserRole.user : UserRole.admin,
                Status = (i % 2 == 0) ? UserStatus.active : UserStatus.inactive,
                CreatedAt = baseTime,
                UpdatedAt = baseTime,
                Nickname = $"nick{i}",
                Name = $"Name{i}",
                Lastname = $"Lastname{i}",
                RegistrationDate = baseTime
            });
        }

        var stopwatch = Stopwatch.StartNew();

        // Act
        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        stopwatch.Stop();

        // Assert
        var savedUsersCount = await _context.Users.CountAsync();
        savedUsersCount.Should().Be(100);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000);
    }

    [Fact]
    public async Task Context_ShouldHandleComplexQueries()
    {
        // Arrange
        await SeedTestDataAsync();

        var stopwatch = Stopwatch.StartNew();

        // Act - Query with includes
        var activeUsersWithSessions = await _context.Users
            .Include(u => u.Sessions)
            .Include(u => u.Preference)
            .Where(u => u.Status == UserStatus.active)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        activeUsersWithSessions.Should().NotBeEmpty();
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(1000);
    }

    [Fact]
    public async Task Context_ShouldHandlePaginationEfficiently()
    {
        // Arrange
        await SeedTestDataAsync();

        var pageSize = 5;
        var pageNumber = 2;
        var stopwatch = Stopwatch.StartNew();

        // Act
        var pagedUsers = await _context.Users
            .OrderBy(u => u.Email)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        stopwatch.Stop();

        // Assert
        pagedUsers.Should().HaveCount(pageSize);
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(500);
    }

    [Fact]
    public async Task Context_ShouldHandleUpdateOperationsEfficiently()
    {
        // Arrange
        await SeedTestDataAsync();

        var usersToUpdate = await _context.Users
            .Where(u => u.Status == UserStatus.active)
            .Take(5)
            .ToListAsync();

        var stopwatch = Stopwatch.StartNew();

        // Act
        foreach (var user in usersToUpdate)
        {
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        stopwatch.Stop();

        // Assert
        stopwatch.ElapsedMilliseconds.Should().BeLessThan(2000);
    }

    private async Task SeedTestDataAsync()
    {
        var users = new List<User>();
        var preferences = new List<Preference>();
        var sessions = new List<Session>();
        var baseTime = DateTime.UtcNow;

        // Create 10 test users
        for (int i = 0; i < 10; i++)
        {
            var user = new User
            {
                Email = $"user{i}@example.com",
                Password = $"hash{i}",
                Role = (i % 2 == 0) ? UserRole.user : UserRole.admin,
                Status = (i % 2 == 0) ? UserStatus.active : UserStatus.inactive,
                CreatedAt = baseTime,
                UpdatedAt = baseTime,
                Nickname = $"nick{i}",
                Name = $"Name{i}",
                Lastname = $"Lastname{i}",
                RegistrationDate = baseTime
            };
            users.Add(user);
        }

        _context.Users.AddRange(users);
        await _context.SaveChangesAsync();

        // Create preferences and sessions
        foreach (var user in users)
        {
            if (user.Status == UserStatus.active)
            {
                preferences.Add(new Preference
                {
                    UserId = user.Id,
                    WcagVersion = "2.1",
                    WcagLevel = WcagLevel.AA,
                    Language = Language.en,
                    CreatedAt = baseTime,
                    UpdatedAt = baseTime
                });

                sessions.Add(new Session
                {
                    UserId = user.Id,
                    TokenHash = $"token_{user.Id}_{Guid.NewGuid()}",
                    CreatedAt = baseTime,
                    ExpiresAt = baseTime.AddHours(24)
                });
            }
        }

        _context.Preferences.AddRange(preferences);
        _context.Sessions.AddRange(sessions);
        await _context.SaveChangesAsync();
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