using System.Text;
using BC = BCrypt.Net.BCrypt;

namespace Users.Application.Services;

public interface IPasswordService
{
    string Hash(string plain);
    bool Verify(string plain, string hash);
}

public sealed class BcryptPasswordService : IPasswordService
{
    public string Hash(string plain) => BC.HashPassword(plain);
    public bool Verify(string plain, string hash) => BC.Verify(plain, hash);
}