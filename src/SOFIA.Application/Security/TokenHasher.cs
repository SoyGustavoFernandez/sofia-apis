using System.Security.Cryptography;
using System.Text;

namespace SOFIA.Application.Security;

internal static class TokenHasher
{
    internal static (string RawToken, string TokenHash) GenerateRefreshToken()
    {
        var raw = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        return (raw, HashToken(raw));
    }

    internal static string HashToken(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));
}
