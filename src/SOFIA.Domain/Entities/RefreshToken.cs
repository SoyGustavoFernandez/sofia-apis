using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class RefreshToken : BaseEntity
{
    private RefreshToken() { }

    public Guid CuentaId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; private set; }
    public bool IsRevoked { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }

    public static RefreshToken Create(Guid cuentaId, string tokenHash, DateTimeOffset expiresAt) =>
        new()
        {
            CuentaId = cuentaId,
            TokenHash = tokenHash,
            ExpiresAt = expiresAt
        };

    public void Revoke()
    {
        IsRevoked = true;
        RevokedAt = DateTimeOffset.UtcNow;
    }
}
