using FluentAssertions;
using Users.Domain.Entities;
using Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Users.Tests.Infrastructure;

public class EntityConfigurationTests : IDisposable
{
    private readonly UsersDbContext _context;
    private bool _disposed;

    public EntityConfigurationTests()
    {
        var options = new DbContextOptionsBuilder<UsersDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new UsersDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public void UserEntity_ShouldHaveCorrectConstraints()
    {
        // Arrange & Act
        var entityType = _context.Model.FindEntityType(typeof(User));

        // Assert
        entityType.Should().NotBeNull();

        // Verificar propiedades obligatorias
        var emailProperty = entityType!.FindProperty(nameof(User.Email));
        emailProperty.Should().NotBeNull();
        emailProperty!.IsNullable.Should().BeFalse();

        var passwordProperty = entityType.FindProperty(nameof(User.Password));
        passwordProperty.Should().NotBeNull();
        passwordProperty!.IsNullable.Should().BeFalse();

        var roleProperty = entityType.FindProperty(nameof(User.Role));
        roleProperty.Should().NotBeNull();
        roleProperty!.IsNullable.Should().BeFalse();

        var statusProperty = entityType.FindProperty(nameof(User.Status));
        statusProperty.Should().NotBeNull();
        statusProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void UserEntity_ShouldHaveCorrectIndexes()
    {
        // Arrange & Act
        var entityType = _context.Model.FindEntityType(typeof(User));

        // Assert
        entityType.Should().NotBeNull();

        // Verificar que existe índice único en Email
        var emailIndex = entityType!.GetIndexes()
            .FirstOrDefault(i => i.Properties.Any(p => p.Name == nameof(User.Email)));

        emailIndex.Should().NotBeNull();
        emailIndex!.IsUnique.Should().BeTrue();
    }

    [Fact]
    public void PreferenceEntity_ShouldHaveCorrectConstraints()
    {
        // Arrange & Act
        var entityType = _context.Model.FindEntityType(typeof(Preference));

        // Assert
        entityType.Should().NotBeNull();

        // Verificar propiedades obligatorias
        var userIdProperty = entityType!.FindProperty(nameof(Preference.UserId));
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();

        var wcagVersionProperty = entityType.FindProperty(nameof(Preference.WcagVersion));
        wcagVersionProperty.Should().NotBeNull();
        wcagVersionProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void SessionEntity_ShouldHaveCorrectConstraints()
    {
        // Arrange & Act
        var entityType = _context.Model.FindEntityType(typeof(Session));

        // Assert
        entityType.Should().NotBeNull();

        // Verificar propiedades obligatorias
        var userIdProperty = entityType!.FindProperty(nameof(Session.UserId));
        userIdProperty.Should().NotBeNull();
        userIdProperty!.IsNullable.Should().BeFalse();

        var tokenHashProperty = entityType.FindProperty(nameof(Session.TokenHash));
        tokenHashProperty.Should().NotBeNull();
        tokenHashProperty!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void DatabaseModel_ShouldHaveCorrectEntityCount()
    {
        // Arrange & Act
        var entityTypes = _context.Model.GetEntityTypes().ToList();

        // Assert
        entityTypes.Should().HaveCount(3); // User, Preference, Session

        var entityNames = entityTypes.Select(e => e.ClrType.Name).ToList();
        entityNames.Should().Contain("User");
        entityNames.Should().Contain("Preference");
        entityNames.Should().Contain("Session");
    }

    [Fact]
    public void Context_ShouldHandleEnumConversions()
    {
        // Arrange
        var user = new User
        {
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = UserRole.user,
            Status = UserStatus.active,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Nickname = "test",
            Name = "Test",
            Lastname = "User",
            RegistrationDate = DateTime.UtcNow
        };

        // Act
        _context.Users.Add(user);
        _context.SaveChanges();

        var retrievedUser = _context.Users.First();

        // Assert
        retrievedUser.Role.Should().Be(UserRole.user);
        retrievedUser.Status.Should().Be(UserStatus.active);
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