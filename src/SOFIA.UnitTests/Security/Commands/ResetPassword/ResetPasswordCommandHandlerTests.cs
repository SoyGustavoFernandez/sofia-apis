using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security.Commands.ResetPassword;
using SOFIA.Domain.Entities;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.UnitTests.Security.Commands.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private const string RawRecoveryToken = "raw-recovery-token";

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "old_hash").Value!;
    private readonly DomainRefreshToken _activeToken;
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<AuditoriaEventoSeguridad>> _auditoriaMock = new List<AuditoriaEventoSeguridad>().BuildMockDbSet();
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _cuenta.GenerateRecoveryToken(Hash(RawRecoveryToken));
        _activeToken = DomainRefreshToken.Create(_cuenta.Id, "hash", DateTimeOffset.UtcNow.AddDays(7));

        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.RefreshTokens).Returns(new List<DomainRefreshToken> { _activeToken }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.AuditoriasEventosSeguridad).Returns(_auditoriaMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("new_hash");

        _handler = new ResetPasswordCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object, Mock.Of<ICurrentUser>());
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    [Fact]
    public async Task Handle_ShouldRevokeRefreshTokens_WhenResetSucceeds()
    {
        var command = new ResetPasswordCommand("usuario", RawRecoveryToken, "NuevaClave123");

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.PasswordHash.Should().Be("new_hash");
        _ = _activeToken.IsRevoked.Should().BeTrue(because: "a stolen refresh token must not survive a password reset");
    }

    [Fact]
    public async Task Handle_ShouldRecordAuditEventForOwner_WhenResetSucceeds()
    {
        var result = await _handler.Handle(new ResetPasswordCommand("usuario", RawRecoveryToken, "NuevaClave123"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _auditoriaMock.Verify(s => s.Add(It.Is<AuditoriaEventoSeguridad>(a =>
            a.TipoAccion == AuditEventos.ClaveRestablecer && a.RegistroIdAfectado == _cuenta.Id && a.EmpleadoId == _cuenta.EmpleadoId && a.PayloadNuevo == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotRecordAuditEvent_WhenRecoveryTokenIsInvalid()
    {
        _ = await _handler.Handle(new ResetPasswordCommand("usuario", "token-invalido", "NuevaClave123"), CancellationToken.None);

        _auditoriaMock.Verify(s => s.Add(It.IsAny<AuditoriaEventoSeguridad>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenStoredHashIsPresentedAsToken()
    {
        var command = new ResetPasswordCommand("usuario", _cuenta.RecoveryToken!, "NuevaClave123");

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue(because: "a leaked database value must not work as a recovery token");
        _ = _cuenta.PasswordHash.Should().Be("old_hash");
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRecoveryTokenIsReused()
    {
        var command = new ResetPasswordCommand("usuario", RawRecoveryToken, "NuevaClave123");
        _ = await _handler.Handle(command, CancellationToken.None);

        var reuse = await _handler.Handle(command with { NewPassword = "OtraClave123" }, CancellationToken.None);

        _ = reuse.IsFailure.Should().BeTrue(because: "a recovery token is single use");
        _ = reuse.Error.Should().Be(Cuenta.InvalidRecoveryTokenError);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRecoveryTokenExpired()
    {
        typeof(Cuenta).GetField("<RecoveryTokenExpiry>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(_cuenta, DateTimeOffset.UtcNow.AddMinutes(-1));

        var result = await _handler.Handle(new ResetPasswordCommand("usuario", RawRecoveryToken, "NuevaClave123"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = _cuenta.PasswordHash.Should().Be("old_hash");
        _ = _activeToken.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldKeepRefreshTokens_WhenRecoveryTokenIsInvalid()
    {
        var command = new ResetPasswordCommand("usuario", "token-invalido", "NuevaClave123");

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = _activeToken.IsRevoked.Should().BeFalse();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownUsername_ReturnsSameErrorAsInvalidToken()
    {
        var unknown = await _handler.Handle(new ResetPasswordCommand("no-existe", "token", "NuevaClave123"), CancellationToken.None);
        var badToken = await _handler.Handle(new ResetPasswordCommand("usuario", "token-invalido", "NuevaClave123"), CancellationToken.None);

        _ = unknown.IsFailure.Should().BeTrue();
        _ = unknown.Error.Should().Be(badToken.Error, because: "the response must not reveal whether the account exists");
    }

    [Fact]
    public async Task Handle_UnknownUsername_StillHashesPassword()
    {
        _ = await _handler.Handle(new ResetPasswordCommand("no-existe", "token", "NuevaClave123"), CancellationToken.None);

        _passwordHasherMock.Verify(p => p.Hash("NuevaClave123"), Times.Once, "skipping the hash would leak account existence through timing");
    }
}
