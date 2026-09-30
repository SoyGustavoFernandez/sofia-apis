using System.Text;

namespace SOFIA.Infrastructure.Authentication;

public class JwtOptions
{
    // HS256 needs a key of at least 256 bits
    public const int MinSecretKeyBytes = 32;

    public string SecretKey { get; init; } = string.Empty;
    public string Issuer { get; init; } = string.Empty;
    public string Audience { get; init; } = string.Empty;
    public int ExpiryMinutes { get; init; }

    public void EnsureValid()
    {
        if (string.IsNullOrEmpty(SecretKey))
        {
            throw new InvalidOperationException("CRITICAL: JWT SecretKey is not configured. Provide Jwt:SecretKey via environment secrets.");
        }

        if (Encoding.UTF8.GetByteCount(SecretKey) < MinSecretKeyBytes)
        {
            throw new InvalidOperationException($"CRITICAL: JWT SecretKey must be at least {MinSecretKeyBytes} bytes long.");
        }

        if (string.IsNullOrWhiteSpace(Issuer) || string.IsNullOrWhiteSpace(Audience))
        {
            throw new InvalidOperationException("CRITICAL: JWT Issuer and Audience must be configured.");
        }
    }
}
