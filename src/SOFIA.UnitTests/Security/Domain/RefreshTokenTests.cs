using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Domain;

public class RefreshTokenTests
{
    [Fact]
    public void IsReuseAttempt_ShouldReturnFalse_WhenTokenIsActive()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));

        _ = token.IsReuseAttempt(DateTimeOffset.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsReuseAttempt_ShouldReturnFalse_WhenRevokedWithinGracePeriod()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        token.Revoke();

        _ = token.IsReuseAttempt(DateTimeOffset.UtcNow).Should().BeFalse(because: "a concurrent refresh from another tab is not theft");
    }

    [Fact]
    public void IsReuseAttempt_ShouldReturnTrue_WhenRevokedBeforeGracePeriod()
    {
        var token = RefreshToken.Create(Guid.NewGuid(), "hash", DateTimeOffset.UtcNow.AddDays(7));
        token.Revoke();

        var afterGrace = token.RevokedAt!.Value + RefreshToken.ReuseGracePeriod + TimeSpan.FromSeconds(1);

        _ = token.IsReuseAttempt(afterGrace).Should().BeTrue();
    }
}
