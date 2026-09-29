using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.ChangePassword;
using SOFIA.Domain.Entities;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.UnitTests.Security.Commands.ChangePassword;

public class ChangePasswordCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "old_hash").Value!;
    private readonly DomainRefreshToken _tokenA;
    private readonly DomainRefreshToken _tokenB;
    private readonly ChangePasswordCommandHandler _handler;

    public ChangePasswordCommandHandlerTests()
    {
        _tokenA = DomainRefreshToken.Create(_cuenta.Id, "hash-a", DateTimeOffset.UtcNow.AddDays(7));
        _tokenB = DomainRefreshToken.Create(_cuenta.Id, "hash-b", DateTimeOffset.UtcNow.AddDays(7));

        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.RefreshTokens).Returns(new List<DomainRefreshToken> { _tokenA, _tokenB }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _passwordHasherMock.Setup(p => p.Verify("ClaveActual1", "old_hash")).Returns(true);
        _ = _passwordHasherMock.Setup(p => p.Hash("ClaveNueva1")).Returns("new_hash");

        _handler = new ChangePasswordCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldUpdatePasswordAndRevokeAllSessions_WhenCurrentPasswordIsCorrect()
    {
        var stamp = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new ChangePasswordCommand(_cuenta.Id, "ClaveActual1", "ClaveNueva1"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.PasswordHash.Should().Be("new_hash");
        _ = _cuenta.RequiereCambioClave.Should().BeFalse();
        _ = _cuenta.SecurityStamp.Should().NotBe(stamp, because: "live access tokens must stop working");
        _ = _tokenA.IsRevoked.Should().BeTrue();
        _ = _tokenB.IsRevoked.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldFailWithoutChanges_WhenCurrentPasswordIsWrong()
    {
        var result = await _handler.Handle(new ChangePasswordCommand(_cuenta.Id, "Incorrecta1", "ClaveNueva1"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(400, because: "a 401 would make the client treat it as an expired session");
        _ = result.Error.Code.Should().Be("Auth.ClaveActualIncorrecta");
        _ = _cuenta.PasswordHash.Should().Be("old_hash");
        _ = _tokenA.IsRevoked.Should().BeFalse();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenAccountIsInactive()
    {
        _cuenta.ToggleActive();

        var result = await _handler.Handle(new ChangePasswordCommand(_cuenta.Id, "ClaveActual1", "ClaveNueva1"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(401);
        _ = _cuenta.PasswordHash.Should().Be("old_hash");
    }
}
