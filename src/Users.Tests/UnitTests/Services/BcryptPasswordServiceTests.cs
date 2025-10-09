using Xunit;
using FluentAssertions;
using Users.Application.Services;

namespace Users.Tests.UnitTests.Services;

public class BcryptPasswordServiceTests
{
    private readonly BcryptPasswordService _passwordService;

    public BcryptPasswordServiceTests()
    {
        _passwordService = new BcryptPasswordService();
    }

    [Fact]
    public void Hash_ShouldReturnDifferentHashForSamePassword()
    {
        // Arrange
        const string password = "testPassword123!";

        // Act
        var hash1 = _passwordService.Hash(password);
        var hash2 = _passwordService.Hash(password);

        // Assert
        hash1.Should().NotBeNullOrEmpty();
        hash2.Should().NotBeNullOrEmpty();
        hash1.Should().NotBe(hash2); // BCrypt genera salts diferentes cada vez
        hash1.Length.Should().Be(60); // BCrypt siempre genera hashes de 60 caracteres
    }

    [Theory]
    [InlineData("simplePassword")]
    [InlineData("ComplexPassword123!")]
    [InlineData("ñáéíóú@#$%")]
    [InlineData("")]
    public void Hash_ShouldHandleDifferentPasswordTypes(string password)
    {
        // Act
        var hash = _passwordService.Hash(password);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Length.Should().Be(60);
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void Verify_ShouldReturnTrueForCorrectPassword()
    {
        // Arrange
        const string password = "testPassword123!";
        var hash = _passwordService.Hash(password);

        // Act
        var result = _passwordService.Verify(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void Verify_ShouldReturnFalseForIncorrectPassword()
    {
        // Arrange
        const string correctPassword = "testPassword123!";
        const string incorrectPassword = "wrongPassword123!";
        var hash = _passwordService.Hash(correctPassword);

        // Act
        var result = _passwordService.Verify(incorrectPassword, hash);

        // Assert
        result.Should().BeFalse();
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("password", "")]
    [InlineData("", "$2a$10$invalidhash")]
    public void Verify_ShouldHandleEdgeCases(string password, string hash)
    {
        // Act & Assert
        try
        {
            var result = _passwordService.Verify(password, hash);
            result.Should().BeFalse();
        }
        catch (Exception ex)
        {
            // BCrypt puede lanzar excepciones para hashes inválidos o strings vacíos
            (ex is ArgumentException || ex is ArgumentOutOfRangeException || ex is BCrypt.Net.SaltParseException)
                .Should().BeTrue("BCrypt should throw known exceptions for invalid inputs");
        }
    }

    [Fact]
    public void Verify_ShouldReturnFalseForInvalidHash()
    {
        // Arrange
        const string password = "testPassword123!";
        const string invalidHash = "invalidHashString";

        // Act & Assert
        try
        {
            var result = _passwordService.Verify(password, invalidHash);
            result.Should().BeFalse();
        }
        catch (Exception ex)
        {
            // BCrypt puede lanzar excepciones para hashes inválidos
            (ex is BCrypt.Net.SaltParseException || ex is ArgumentException || ex is ArgumentOutOfRangeException)
                .Should().BeTrue("BCrypt should throw known exceptions for invalid hashes");
        }
    }
}