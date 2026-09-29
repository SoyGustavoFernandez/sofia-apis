using System.Reflection;
using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Domain;

public class CuentaPasswordTests
{
    private const string TokenHash = "0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF0123456789ABCDEF";

    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "old_hash").Value!;

    private void SetPrivate(string property, object? value) =>
        typeof(Cuenta).GetField($"<{property}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(_cuenta, value);

    [Fact]
    public void UpdatePassword_ShouldRotateStampAndClearForcedChange()
    {
        var stamp = _cuenta.SecurityStamp;
        _cuenta.GenerateRecoveryToken(TokenHash);

        _cuenta.UpdatePassword("new_hash");

        _ = _cuenta.PasswordHash.Should().Be("new_hash");
        _ = _cuenta.SecurityStamp.Should().NotBe(stamp);
        _ = _cuenta.RequiereCambioClave.Should().BeFalse();
        _ = _cuenta.RecoveryToken.Should().BeNull(because: "a pending recovery token must not outlive a password change");
    }

    [Fact]
    public void GenerateRecoveryToken_ShouldStoreOnlyTheGivenHash()
    {
        _cuenta.GenerateRecoveryToken(TokenHash);

        _ = _cuenta.RecoveryToken.Should().Be(TokenHash);
        _ = _cuenta.RecoveryTokenExpiry.Should().BeCloseTo(DateTimeOffset.UtcNow.AddHours(1), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void ResetPassword_ShouldSucceedOnce_WhenHashMatches()
    {
        _cuenta.GenerateRecoveryToken(TokenHash);

        var first = _cuenta.ResetPassword(TokenHash, "new_hash");
        var second = _cuenta.ResetPassword(TokenHash, "other_hash");

        _ = first.IsSuccess.Should().BeTrue();
        _ = _cuenta.RequiereCambioClave.Should().BeFalse(because: "the user chose this password");
        _ = second.IsFailure.Should().BeTrue(because: "a recovery token is single use");
        _ = _cuenta.PasswordHash.Should().Be("new_hash");
    }

    [Fact]
    public void ResetPassword_ShouldFail_WhenHashDiffers()
    {
        _cuenta.GenerateRecoveryToken(TokenHash);

        var result = _cuenta.ResetPassword(TokenHash.ToLowerInvariant(), "new_hash");

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Should().Be(Cuenta.InvalidRecoveryTokenError);
    }

    [Fact]
    public void ResetPassword_ShouldFail_WhenNoTokenWasIssued()
    {
        var result = _cuenta.ResetPassword(TokenHash, "new_hash");

        _ = result.IsFailure.Should().BeTrue();
        _ = _cuenta.PasswordHash.Should().Be("old_hash");
    }

    [Fact]
    public void ResetPassword_ShouldFail_WhenTokenExpired()
    {
        _cuenta.GenerateRecoveryToken(TokenHash);
        SetPrivate(nameof(Cuenta.RecoveryTokenExpiry), DateTimeOffset.UtcNow.AddSeconds(-1));

        var result = _cuenta.ResetPassword(TokenHash, "new_hash");

        _ = result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void RegisterFailedAttempt_ShouldLock_AfterFiveFailures()
    {
        for (var i = 0; i < 5; i++)
        {
            _cuenta.RegisterFailedAttempt();
        }

        _ = _cuenta.BloqueadoHasta.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(15), TimeSpan.FromMinutes(1));
    }

    [Fact]
    public void RegisterFailedAttempt_ShouldStartFreshCount_WhenLockExpired()
    {
        for (var i = 0; i < 5; i++)
        {
            _cuenta.RegisterFailedAttempt();
        }

        SetPrivate(nameof(Cuenta.BloqueadoHasta), DateTimeOffset.UtcNow.AddSeconds(-1));

        _cuenta.RegisterFailedAttempt();

        _ = _cuenta.IntentosFallidos.Should().Be(1);
        _ = _cuenta.BloqueadoHasta.Should().BeNull(because: "a single failure after the lock expired must not re-lock the account");
    }

    [Fact]
    public void RegisterFailedAttempt_ShouldLockAgain_AfterFiveFreshFailures()
    {
        for (var i = 0; i < 5; i++)
        {
            _cuenta.RegisterFailedAttempt();
        }

        SetPrivate(nameof(Cuenta.BloqueadoHasta), DateTimeOffset.UtcNow.AddSeconds(-1));

        for (var i = 0; i < 4; i++)
        {
            _cuenta.RegisterFailedAttempt();
        }

        _ = _cuenta.BloqueadoHasta.Should().BeNull();

        _cuenta.RegisterFailedAttempt();

        _ = _cuenta.BloqueadoHasta.Should().BeAfter(DateTimeOffset.UtcNow);
    }
}
