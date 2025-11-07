using FluentAssertions;
using Users.Tests.Helpers;
using Users.Domain.Entities;

namespace Users.Tests.Domain;

public class DomainEntitiesTests
{
    [Fact]
    public void User_Properties_SetAndGet_ShouldWork()
    {
        // Arrange & Act
        var now = DateTimeHelper.EcuadorNow;
        var user = new User
        {
            Id = 1,
            Nickname = "testuser",
            Name = "Test",
            Lastname = "User",
            Email = "test@example.com",
            Password = "hashedpassword",
            Role = UserRole.admin,
            Status = UserStatus.active,
            EmailConfirmed = true,
            LastLogin = now,
            RegistrationDate = now.AddDays(-30),
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now
        };

        // Assert
        user.Id.Should().Be(1);
        user.Nickname.Should().Be("testuser");
        user.Name.Should().Be("Test");
        user.Lastname.Should().Be("User");
        user.Email.Should().Be("test@example.com");
        user.Password.Should().Be("hashedpassword");
        user.Role.Should().Be(UserRole.admin);
        user.Status.Should().Be(UserStatus.active);
        user.EmailConfirmed.Should().BeTrue();
        user.LastLogin.Should().NotBeNull();
        user.RegistrationDate.Should().BeBefore(DateTimeHelper.EcuadorNow);
        user.CreatedAt.Should().BeBefore(DateTimeHelper.EcuadorNow);
        // Comparar con hora de Ecuador (UTC-5)
        user.UpdatedAt.Should().BeCloseTo(DateTimeHelper.EcuadorNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void User_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var user = new User();

        // Assert
        user.Role.Should().Be(UserRole.user);
        user.Status.Should().Be(UserStatus.active);
        user.EmailConfirmed.Should().BeFalse();
        user.Sessions.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void User_NavigationProperties_ShouldWork()
    {
        // Arrange
        var user = new User();
        var preference = new Preference { WcagVersion = "2.1" };
        var session = new Session { TokenHash = "test-token" };

        // Act
        user.Preference = preference;
        user.Sessions.Add(session);

        // Assert
        user.Preference.Should().Be(preference);
        user.Sessions.Should().Contain(session);
        user.Sessions.Count.Should().Be(1);
    }

    [Fact]
    public void Preference_Properties_SetAndGet_ShouldWork()
    {
        // Arrange & Act
        var now = DateTimeHelper.EcuadorNow;
        var preference = new Preference
        {
            Id = 1,
            UserId = 10,
            WcagVersion = "2.1",
            WcagLevel = WcagLevel.AA,
            Language = Language.en,
            VisualTheme = VisualTheme.dark,
            ReportFormat = ReportFormat.html,
            NotificationsEnabled = false,
            AiResponseLevel = AiResponseLevel.detailed,
            FontSize = 16,
            CreatedAt = now.AddDays(-1),
            UpdatedAt = now
        };

        // Assert
        preference.Id.Should().Be(1);
        preference.UserId.Should().Be(10);
        preference.WcagVersion.Should().Be("2.1");
        preference.WcagLevel.Should().Be(WcagLevel.AA);
        preference.Language.Should().Be(Language.en);
        preference.VisualTheme.Should().Be(VisualTheme.dark);
        preference.ReportFormat.Should().Be(ReportFormat.html);
        preference.NotificationsEnabled.Should().BeFalse();
        preference.AiResponseLevel.Should().Be(AiResponseLevel.detailed);
        preference.FontSize.Should().Be(16);
        preference.CreatedAt.Should().BeBefore(DateTimeHelper.EcuadorNow);
        // Comparar con hora de Ecuador (UTC-5)
        preference.UpdatedAt.Should().BeCloseTo(DateTimeHelper.EcuadorNow, TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void Preference_DefaultValues_ShouldBeSet()
    {
        // Arrange & Act
        var preference = new Preference { WcagVersion = "2.1" }; // Required property

        // Assert
        preference.Language.Should().Be(Language.es);
        preference.VisualTheme.Should().Be(VisualTheme.light);
        preference.ReportFormat.Should().Be(ReportFormat.pdf);
        preference.NotificationsEnabled.Should().BeTrue();
        preference.FontSize.Should().Be(14);
        preference.AiResponseLevel.Should().BeNull();
    }

    [Fact]
    public void Preference_NavigationProperty_ShouldWork()
    {
        // Arrange
        var preference = new Preference { WcagVersion = "2.1" };
        var user = new User { Nickname = "testuser" };

        // Act
        preference.User = user;

        // Assert
        preference.User.Should().Be(user);
    }

    [Fact]
    public void Session_Properties_SetAndGet_ShouldWork()
    {
        // Arrange & Act
        var session = new Session
        {
            Id = 1,
            UserId = 5,
            TokenHash = "hashed-token-value",
            CreatedAt = DateTime.UtcNow.AddHours(-2),
            ExpiresAt = DateTime.UtcNow.AddHours(2)
        };

        // Assert
        session.Id.Should().Be(1);
        session.UserId.Should().Be(5);
        session.TokenHash.Should().Be("hashed-token-value");
        session.CreatedAt.Should().BeBefore(DateTime.UtcNow);
        session.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void Session_NavigationProperty_ShouldWork()
    {
        // Arrange
        var session = new Session();
        var user = new User { Nickname = "sessionuser" };

        // Act
        session.User = user;

        // Assert
        session.User.Should().Be(user);
    }

    [Theory]
    [InlineData(UserRole.admin)]
    [InlineData(UserRole.user)]
    public void UserRole_EnumValues_ShouldWork(UserRole role)
    {
        // Arrange
        var user = new User();

        // Act
        user.Role = role;

        // Assert
        user.Role.Should().Be(role);
    }

    [Theory]
    [InlineData(UserStatus.active)]
    [InlineData(UserStatus.inactive)]
    [InlineData(UserStatus.blocked)]
    public void UserStatus_EnumValues_ShouldWork(UserStatus status)
    {
        // Arrange
        var user = new User();

        // Act
        user.Status = status;

        // Assert
        user.Status.Should().Be(status);
    }

    [Theory]
    [InlineData(WcagLevel.A)]
    [InlineData(WcagLevel.AA)]
    [InlineData(WcagLevel.AAA)]
    public void WcagLevel_EnumValues_ShouldWork(WcagLevel level)
    {
        // Arrange
        var preference = new Preference { WcagVersion = "2.1" };

        // Act
        preference.WcagLevel = level;

        // Assert
        preference.WcagLevel.Should().Be(level);
    }

    [Theory]
    [InlineData(Language.es)]
    [InlineData(Language.en)]
    public void Language_EnumValues_ShouldWork(Language language)
    {
        // Arrange
        var preference = new Preference { WcagVersion = "2.1" };

        // Act
        preference.Language = language;

        // Assert
        preference.Language.Should().Be(language);
    }

    [Theory]
    [InlineData(VisualTheme.light)]
    [InlineData(VisualTheme.dark)]
    public void VisualTheme_EnumValues_ShouldWork(VisualTheme theme)
    {
        // Arrange
        var preference = new Preference { WcagVersion = "2.1" };

        // Act
        preference.VisualTheme = theme;

        // Assert
        preference.VisualTheme.Should().Be(theme);
    }

    [Theory]
    [InlineData(ReportFormat.pdf)]
    [InlineData(ReportFormat.html)]
    [InlineData(ReportFormat.json)]
    [InlineData(ReportFormat.excel)]
    public void ReportFormat_EnumValues_ShouldWork(ReportFormat format)
    {
        // Arrange
        var preference = new Preference { WcagVersion = "2.1" };

        // Act
        preference.ReportFormat = format;

        // Assert
        preference.ReportFormat.Should().Be(format);
    }

    [Theory]
    [InlineData(AiResponseLevel.basic)]
    [InlineData(AiResponseLevel.intermediate)]
    [InlineData(AiResponseLevel.detailed)]
    public void AiResponseLevel_EnumValues_ShouldWork(AiResponseLevel level)
    {
        // Arrange
        var preference = new Preference { WcagVersion = "2.1" };

        // Act
        preference.AiResponseLevel = level;

        // Assert
        preference.AiResponseLevel.Should().Be(level);
    }

    [Fact]
    public void AiResponseLevel_NullValue_ShouldWork()
    {
        // Arrange
        var preference = new Preference { WcagVersion = "2.1" };

        // Act
        preference.AiResponseLevel = null;

        // Assert
        preference.AiResponseLevel.Should().BeNull();
    }

    [Fact]
    public void Entities_ComplexScenario_ShouldWork()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Nickname = "johndoe",
            Name = "John",
            Lastname = "Doe",
            Email = "john@example.com",
            Password = "hashed",
            Role = UserRole.user,
            Status = UserStatus.active,
            EmailConfirmed = true,
            RegistrationDate = DateTime.UtcNow.AddDays(-10),
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow
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
            CreatedAt = DateTime.UtcNow.AddDays(-9),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var session1 = new Session
        {
            Id = 1,
            UserId = 1,
            TokenHash = "token1-hash",
            CreatedAt = DateTime.UtcNow.AddHours(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(23)
        };

        var session2 = new Session
        {
            Id = 2,
            UserId = 1,
            TokenHash = "token2-hash",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired
        };

        // Act - Set up relationships
        user.Preference = preference;
        preference.User = user;

        user.Sessions.Add(session1);
        user.Sessions.Add(session2);
        session1.User = user;
        session2.User = user;

        // Assert
        user.Preference.Should().Be(preference);
        preference.User.Should().Be(user);
        user.Sessions.Should().HaveCount(2);
        user.Sessions.Should().Contain(session1);
        user.Sessions.Should().Contain(session2);
        session1.User.Should().Be(user);
        session2.User.Should().Be(user);

        // Verify relationships work both ways
        preference.UserId.Should().Be(user.Id);
        session1.UserId.Should().Be(user.Id);
        session2.UserId.Should().Be(user.Id);
    }
}