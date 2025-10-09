using Xunit;
using System.Security.Claims;
using Users.Application.Services;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace Users.Tests.Services;

public class JwtTokenServiceTests
{
    private readonly IJwtTokenService _jwtService;
    private readonly IConfiguration _configuration;

    public JwtTokenServiceTests()
    {
        // Configuración de prueba con valores válidos
        var inMemorySettings = new Dictionary<string, string>
        {
            {"JwtSettings:SecretKey", "ThisIsAVerySecureSecretKeyForTesting1234567890"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"},
            {"JwtSettings:ExpiryHours", "24"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings!)
            .Build();

        _jwtService = new JwtTokenService(_configuration);
    }

    [Fact]
    public void Constructor_WithValidConfiguration_ShouldCreateService()
    {
        // Act & Assert
        Assert.NotNull(_jwtService);
    }

    [Fact]
    public void Constructor_WithoutSecretKey_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>()!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(invalidConfig));
        Assert.Contains("SecretKey is required", exception.Message);
    }

    [Fact]
    public void Constructor_WithShortSecretKey_ShouldThrowException()
    {
        // Arrange
        var invalidConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "short"}
            }!)
            .Build();

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() =>
            new JwtTokenService(invalidConfig));
        Assert.Contains("at least 32 characters", exception.Message);
    }

    [Fact]
    public void GenerateToken_WithValidData_ShouldReturnToken()
    {
        // Arrange
        var userId = 123;
        var email = "test@example.com";
        var role = "Admin";
        var userName = "Test User";

        // Act
        var token = _jwtService.GenerateToken(userId, email, role, userName);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Contains(".", token); // JWT tiene formato xxx.yyy.zzz
    }

    [Fact]
    public void GenerateToken_ShouldIncludeCorrectClaims()
    {
        // Arrange
        var userId = 456;
        var email = "admin@example.com";
        var role = "Admin";
        var userName = "Admin User";

        // Act
        var token = _jwtService.GenerateToken(userId, email, role, userName);

        // Validar token y extraer claims
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal(userId.ToString(), jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal(email, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal(userName, jwtToken.Claims.First(c => c.Type == JwtRegisteredClaimNames.Name).Value);
        Assert.Equal(role, jwtToken.Claims.First(c => c.Type == ClaimTypes.Role).Value);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Jti);
        Assert.Contains(jwtToken.Claims, c => c.Type == JwtRegisteredClaimNames.Iat);
    }

    [Fact]
    public void GenerateToken_ShouldIncludeIssuerAndAudience()
    {
        // Arrange
        var userId = 789;
        var email = "user@example.com";
        var role = "User";
        var userName = "Regular User";

        // Act
        var token = _jwtService.GenerateToken(userId, email, role, userName);

        // Validar token
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Contains("TestAudience", jwtToken.Audiences);
    }

    [Fact]
    public void ValidateToken_WithValidToken_ShouldReturnClaimsPrincipal()
    {
        // Arrange
        var userId = 111;
        var email = "valid@example.com";
        var role = "User";
        var userName = "Valid User";
        var token = _jwtService.GenerateToken(userId, email, role, userName);

        // Act
        var principal = _jwtService.ValidateToken(token);

        // Assert
        Assert.NotNull(principal);
        Assert.NotNull(principal.Identity);
        Assert.True(principal.Identity.IsAuthenticated);

        // Verificar claims - puede estar en Sub o NameIdentifier
        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Assert.Equal(userId.ToString(), userIdClaim);

        var emailClaim = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value
                         ?? principal.FindFirst(ClaimTypes.Email)?.Value;
        Assert.Equal(email, emailClaim);
    }

    [Fact]
    public void ValidateToken_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var principal = _jwtService.ValidateToken(invalidToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithEmptyToken_ShouldReturnNull()
    {
        // Act
        var principal = _jwtService.ValidateToken("");

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_WithMalformedToken_ShouldReturnNull()
    {
        // Arrange - token con formato incorrecto
        var malformedToken = "not.a.valid.jwt.token.at.all";

        // Act
        var principal = _jwtService.ValidateToken(malformedToken);

        // Assert
        Assert.Null(principal);
    }

    [Fact]
    public void GetTokenExpiration_ShouldReturnFutureDate()
    {
        // Arrange
        var before = DateTime.UtcNow;

        // Act
        var expiration = _jwtService.GetTokenExpiration();
        var after = DateTime.UtcNow.AddHours(24);

        // Assert
        Assert.True(expiration > before);
        Assert.True(expiration <= after.AddMinutes(1)); // Margen de 1 minuto
    }

    [Fact]
    public void GetTokenExpiration_ShouldRespectConfiguredExpiryHours()
    {
        // Arrange - servicio con 12 horas de expiración
        var customConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "ThisIsAVerySecureSecretKeyForTesting1234567890"},
                {"JwtSettings:Issuer", "TestIssuer"},
                {"JwtSettings:Audience", "TestAudience"},
                {"JwtSettings:ExpiryHours", "12"}
            }!)
            .Build();

        var customService = new JwtTokenService(customConfig);
        var before = DateTime.UtcNow.AddHours(12);

        // Act
        var expiration = customService.GetTokenExpiration();

        // Assert
        Assert.True(expiration >= before.AddMinutes(-1));
        Assert.True(expiration <= before.AddMinutes(1));
    }

    [Fact]
    public void Constructor_WithDefaultValues_ShouldUseDefaults()
    {
        // Arrange - configuración solo con SecretKey
        var minimalConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                {"JwtSettings:SecretKey", "ThisIsAVerySecureSecretKeyForTesting1234567890"}
            }!)
            .Build();

        // Act
        var service = new JwtTokenService(minimalConfig);
        var token = service.GenerateToken(1, "test@example.com", "User", "Test");

        // Assert
        Assert.NotNull(token);
        var handler = new JwtSecurityTokenHandler();
        var jwtToken = handler.ReadJwtToken(token);
        Assert.Equal("AccessibilityUsersAPI", jwtToken.Issuer);
        Assert.Contains("AccessibilityClients", jwtToken.Audiences);
    }

    [Theory]
    [InlineData(1, "user1@test.com", "Admin", "User One")]
    [InlineData(999, "user999@test.com", "User", "User Nine Nine Nine")]
    [InlineData(12345, "admin@company.com", "Admin", "Admin User")]
    public void GenerateToken_WithVariousInputs_ShouldGenerateValidTokens(
        int userId, string email, string role, string userName)
    {
        // Act
        var token = _jwtService.GenerateToken(userId, email, role, userName);

        // Assert
        Assert.NotNull(token);
        Assert.NotEmpty(token);

        // Validar que el token es válido
        var principal = _jwtService.ValidateToken(token);
        Assert.NotNull(principal);

        // Verificar userId en cualquier claim (Sub o NameIdentifier)
        var userIdClaim = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        Assert.Equal(userId.ToString(), userIdClaim);
    }
}
