using Xunit;
using FluentAssertions;
using Users.Domain.Entities;
using Users.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Users.Tests.UnitTests.Infrastructure
{
    public class UsersDbContextConfigurationTests : IDisposable
    {
        private readonly UsersDbContext _context;

        public UsersDbContextConfigurationTests()
        {
            var options = new DbContextOptionsBuilder<UsersDbContext>()
                .UseInMemoryDatabase(databaseName: $"ConfigTestDb_{Guid.NewGuid()}")
                .Options;
            _context = new UsersDbContext(options);
        }

        [Fact]
        public void DbContext_ShouldHaveCorrectTableNames()
        {
            // Arrange & Act
            var userEntityType = _context.Model.FindEntityType(typeof(User));
            var sessionEntityType = _context.Model.FindEntityType(typeof(Session));
            var preferenceEntityType = _context.Model.FindEntityType(typeof(Preference));

            // Assert
            userEntityType.Should().NotBeNull();
            userEntityType!.GetTableName().Should().BeOneOf("users", "USERS");

            sessionEntityType.Should().NotBeNull();
            sessionEntityType!.GetTableName().Should().BeOneOf("sessions", "SESSIONS");

            preferenceEntityType.Should().NotBeNull();
            preferenceEntityType!.GetTableName().Should().BeOneOf("preferences", "PREFERENCES");
        }

        [Fact]
        public void DbContext_User_ShouldHaveCorrectPrimaryKey()
        {
            // Arrange & Act
            var userEntityType = _context.Model.FindEntityType(typeof(User));
            var primaryKey = userEntityType!.FindPrimaryKey();

            // Assert
            primaryKey.Should().NotBeNull();
            primaryKey!.Properties.Should().HaveCount(1);
            primaryKey.Properties[0].Name.Should().Be("Id");
            primaryKey.Properties[0].ValueGenerated.Should().Be(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.OnAdd);
        }

        [Fact]
        public void DbContext_User_ShouldHaveCorrectRequiredProperties()
        {
            // Arrange & Act
            var userEntityType = _context.Model.FindEntityType(typeof(User));

            // Assert
            var nicknameProperty = userEntityType!.FindProperty("Nickname");
            nicknameProperty.Should().NotBeNull();
            nicknameProperty!.IsNullable.Should().BeFalse();

            var nameProperty = userEntityType.FindProperty("Name");
            nameProperty.Should().NotBeNull();
            nameProperty!.IsNullable.Should().BeFalse();

            var emailProperty = userEntityType.FindProperty("Email");
            emailProperty.Should().NotBeNull();
            emailProperty!.IsNullable.Should().BeFalse();

            var passwordProperty = userEntityType.FindProperty("Password");
            passwordProperty.Should().NotBeNull();
            passwordProperty!.IsNullable.Should().BeFalse();
        }

        [Fact]
        public void DbContext_User_ShouldHaveUniqueConstraints()
        {
            // Arrange & Act
            var userEntityType = _context.Model.FindEntityType(typeof(User));
            var indexes = userEntityType!.GetIndexes().ToList();

            // Assert
            var emailIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "Email"));
            emailIndex.Should().NotBeNull();
            emailIndex!.IsUnique.Should().BeTrue();

            var nicknameIndex = indexes.FirstOrDefault(i => i.Properties.Any(p => p.Name == "Nickname"));
            nicknameIndex.Should().NotBeNull();
            nicknameIndex!.IsUnique.Should().BeTrue();
        }

        [Fact]
        public void DbContext_Session_ShouldHaveCorrectForeignKey()
        {
            // Arrange & Act
            var sessionEntityType = _context.Model.FindEntityType(typeof(Session));
            var userIdProperty = sessionEntityType!.FindProperty("UserId");
            var foreignKeys = sessionEntityType.GetForeignKeys().ToList();

            // Assert
            userIdProperty.Should().NotBeNull();
            userIdProperty!.IsNullable.Should().BeFalse();

            foreignKeys.Should().HaveCount(1);
            var userForeignKey = foreignKeys[0];
            userForeignKey.PrincipalEntityType.ClrType.Should().Be(typeof(User));
            userForeignKey.Properties[0].Name.Should().Be("UserId");
        }

        [Fact]
        public void DbContext_Preference_ShouldHaveCorrectForeignKey()
        {
            // Arrange & Act
            var preferenceEntityType = _context.Model.FindEntityType(typeof(Preference));
            var userIdProperty = preferenceEntityType!.FindProperty("UserId");
            var foreignKeys = preferenceEntityType.GetForeignKeys().ToList();

            // Assert
            userIdProperty.Should().NotBeNull();
            userIdProperty!.IsNullable.Should().BeFalse();

            foreignKeys.Should().HaveCount(1);
            var userForeignKey = foreignKeys[0];
            userForeignKey.PrincipalEntityType.ClrType.Should().Be(typeof(User));
            userForeignKey.Properties[0].Name.Should().Be("UserId");
        }

        [Fact]
        public void DbContext_User_ShouldHaveCorrectNavigationProperties()
        {
            // Arrange & Act
            var userEntityType = _context.Model.FindEntityType(typeof(User));
            var navigations = userEntityType!.GetNavigations().ToList();

            // Assert
            navigations.Should().HaveCount(2);

            var sessionsNavigation = navigations.FirstOrDefault(n => n.Name == "Sessions");
            sessionsNavigation.Should().NotBeNull();
            sessionsNavigation!.IsCollection.Should().BeTrue();

            var preferenceNavigation = navigations.FirstOrDefault(n => n.Name == "Preference");
            preferenceNavigation.Should().NotBeNull();
            preferenceNavigation!.IsCollection.Should().BeFalse();
        }

        [Fact]
        public void DbContext_Session_ShouldHaveCorrectNavigationProperties()
        {
            // Arrange & Act
            var sessionEntityType = _context.Model.FindEntityType(typeof(Session));
            var navigations = sessionEntityType!.GetNavigations().ToList();

            // Assert
            navigations.Should().HaveCount(1);

            var userNavigation = navigations.FirstOrDefault(n => n.Name == "User");
            userNavigation.Should().NotBeNull();
            userNavigation!.IsCollection.Should().BeFalse();
        }

        [Fact]
        public void DbContext_ShouldConfigureEnumConversions()
        {
            // Arrange & Act
            var userEntityType = _context.Model.FindEntityType(typeof(User));

            // Assert
            var roleProperty = userEntityType!.FindProperty("Role");
            roleProperty.Should().NotBeNull();
            roleProperty!.GetValueConverter().Should().NotBeNull();

            var statusProperty = userEntityType.FindProperty("Status");
            statusProperty.Should().NotBeNull();
            statusProperty!.GetValueConverter().Should().NotBeNull();
        }

        [Fact]
        public void DbContext_Preference_ShouldConfigureEnumConversions()
        {
            // Arrange & Act
            var preferenceEntityType = _context.Model.FindEntityType(typeof(Preference));

            // Assert
            var wcagLevelProperty = preferenceEntityType!.FindProperty("WcagLevel");
            wcagLevelProperty.Should().NotBeNull();
            wcagLevelProperty!.GetValueConverter().Should().NotBeNull();

            var languageProperty = preferenceEntityType.FindProperty("Language");
            languageProperty.Should().NotBeNull();
            languageProperty!.GetValueConverter().Should().NotBeNull();

            var visualThemeProperty = preferenceEntityType.FindProperty("VisualTheme");
            visualThemeProperty.Should().NotBeNull();
            visualThemeProperty!.GetValueConverter().Should().NotBeNull();

            var reportFormatProperty = preferenceEntityType.FindProperty("ReportFormat");
            reportFormatProperty.Should().NotBeNull();
            reportFormatProperty!.GetValueConverter().Should().NotBeNull();
        }

        [Fact]
        public void DbContext_ShouldSaveChangesSuccessfully()
        {
            // Arrange
            var user = new User
            {
                Nickname = "testuser",
                Name = "Test",
                Lastname = "User",
                Email = "test@example.com",
                Password = "hashedpassword"
            };

            // Act
            _context.Users.Add(user);
            var result = _context.SaveChanges();

            // Assert
            result.Should().Be(1);
            user.Id.Should().BeGreaterThan(0);
            _context.Users.Should().HaveCount(1);
        }

        private bool _disposed = false;

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
}