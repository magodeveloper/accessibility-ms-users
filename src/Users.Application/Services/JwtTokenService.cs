using System;
using System.Text;
using System.Security.Claims;
using System.Collections.Generic;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Configuration;

namespace Users.Application.Services;

public interface IJwtTokenService
{
    string GenerateToken(int userId, string email, string role, string userName);
    ClaimsPrincipal? ValidateToken(string token);
    DateTime GetTokenExpiration();
}

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly string _issuer;
    private readonly string _audience;
    private readonly int _expiryHours;
    private readonly SymmetricSecurityKey _signingKey;

    public JwtTokenService(IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("JwtSettings");

        var secretKey = jwtSection["SecretKey"]
            ?? throw new InvalidOperationException("JwtSettings:SecretKey is required");
        _issuer = jwtSection["Issuer"] ?? "AccessibilityUsersAPI";
        _audience = jwtSection["Audience"] ?? "AccessibilityClients";
        _expiryHours = jwtSection.GetValue<int>("ExpiryHours", 24);

        if (secretKey.Length < 32)
        {
            throw new InvalidOperationException("JwtSettings:SecretKey must be at least 32 characters");
        }

        // Cachear la clave de firma para optimizar performance
        _signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    public string GenerateToken(int userId, string email, string role, string userName)
    {
        var claims = new List<Claim>
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.Name, userName),
            new Claim(ClaimTypes.Role, role),
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
        };

        var credentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddHours(_expiryHours);

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _issuer,
                ValidAudience = _audience,
                IssuerSigningKey = _signingKey,
                ClockSkew = TimeSpan.FromMinutes(1)
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public DateTime GetTokenExpiration()
    {
        return DateTime.UtcNow.AddHours(_expiryHours);
    }
}
