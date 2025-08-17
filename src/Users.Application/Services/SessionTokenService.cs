using System.Text;
using System.Security.Cryptography;

namespace Users.Application.Services;

public interface ISessionTokenService
{
    (string token, string tokenHash) GenerateToken(); // token plano + sha256(hex) para DB
    string HashToken(string token);
}

public sealed class SessionTokenService : ISessionTokenService
{
    public (string token, string tokenHash) GenerateToken()
    {
        // 32 bytes aleatorios -> base64url
        var bytes = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
        return (token, HashToken(token));
    }

    public string HashToken(string token)
    {
        using var sha = SHA256.Create();
        var data = Encoding.UTF8.GetBytes(token);
        var hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}