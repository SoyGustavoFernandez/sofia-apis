using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.ResetPassword;
using SOFIA.Domain.Entities;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.UnitTests.Security.Commands.ResetPassword;

public class ResetPasswordCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "old_hash").Value!;
    private readonly DomainRefreshToken _activeToken;
    private readonly ResetPasswordCommandHandler _handler;

    public ResetPasswordCommandHandlerTests()
    {
        _cuenta.GenerateRecoveryToken();
        _activeToken = DomainRefreshToken.Create(_cuenta.Id, "hash", DateTimeOffset.UtcNow.AddDays(7));

        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.RefreshTokens).Returns(new List<DomainRefreshToken> { _activeToken }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("new_hash");

        _handler = new ResetPasswordCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldRevokeRefreshTokens_WhenResetSucceeds()
    {
        var command = new ResetPasswordCommand("usuario", _cuenta.RecoveryToken!, "NuevaClave123");

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.PasswordHash.Should().Be("new_hash");
        _ = _activeToken.IsRevoked.Should().BeTrue(because: "a stolen refresh token must not survive a password reset");
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
