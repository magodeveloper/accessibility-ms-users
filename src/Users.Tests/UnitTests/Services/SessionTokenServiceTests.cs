using Xunit;
using FluentAssertions;
using Users.Application.Services;

namespace Users.Tests.UnitTests.Services;

public class SessionTokenServiceTests
{
    private readonly SessionTokenService _tokenService;

    public SessionTokenServiceTests()
    {
        _tokenService = new SessionTokenService();
    }

    [Fact]
    public void GenerateToken_ShouldReturnValidTokenAndHash()
    {
        // Act
        var (token, tokenHash) = _tokenService.GenerateToken();

        // Assert
        token.Should().NotBeNullOrEmpty();
        tokenHash.Should().NotBeNullOrEmpty();

        // Token debe ser base64url válido (sin =, +, /)
        token.Should().NotContain("=");
        token.Should().NotContain("+");
        token.Should().NotContain("/");

        // Hash debe ser hex lowercase de 64 caracteres (SHA256)
        tokenHash.Should().HaveLength(64);
        tokenHash.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Fact]
    public void GenerateToken_ShouldGenerateUniqueTokensEachTime()
    {
        // Act
        var (token1, hash1) = _tokenService.GenerateToken();
        var (token2, hash2) = _tokenService.GenerateToken();
        var (token3, hash3) = _tokenService.GenerateToken();

        // Assert
        token1.Should().NotBe(token2);
        token2.Should().NotBe(token3);
        token1.Should().NotBe(token3);

        hash1.Should().NotBe(hash2);
        hash2.Should().NotBe(hash3);
        hash1.Should().NotBe(hash3);
    }

    [Fact]
    public void HashToken_ShouldReturnConsistentHash()
    {
        // Arrange
        const string token = "testToken123";

        // Act
        var hash1 = _tokenService.HashToken(token);
        var hash2 = _tokenService.HashToken(token);

        // Assert
        hash1.Should().Be(hash2);
        hash1.Should().HaveLength(64);
        hash1.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("simpleToken")]
    [InlineData("ComplexToken123!@#")]
    [InlineData("ñáéíóú")]
    public void HashToken_ShouldHandleDifferentInputs(string token)
    {
        // Act
        var hash = _tokenService.HashToken(token);

        // Assert
        hash.Should().NotBeNullOrEmpty();
        hash.Should().HaveLength(64);
        hash.Should().MatchRegex("^[a-f0-9]+$");
    }

    [Fact]
    public void HashToken_ShouldGenerateDifferentHashesForDifferentTokens()
    {
        // Arrange
        const string token1 = "token1";
        const string token2 = "token2";

        // Act
        var hash1 = _tokenService.HashToken(token1);
        var hash2 = _tokenService.HashToken(token2);

        // Assert
        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void GenerateToken_HashShouldMatchManualHash()
    {
        // Act
        var (token, generatedHash) = _tokenService.GenerateToken();
        var manualHash = _tokenService.HashToken(token);

        // Assert
        generatedHash.Should().Be(manualHash);
    }

    [Fact]
    public void GenerateToken_ShouldProduceBase64UrlSafeTokens()
    {
        // Act & Assert
        for (int i = 0; i < 100; i++)
        {
            var (token, _) = _tokenService.GenerateToken();

            // Verificar que es base64url válido
            token.Should().MatchRegex("^[A-Za-z0-9_-]+$");

            // Longitud debería ser consistente (32 bytes -> ~43 chars en base64url)
            token.Length.Should().BeInRange(43, 44);
        }
    }
}