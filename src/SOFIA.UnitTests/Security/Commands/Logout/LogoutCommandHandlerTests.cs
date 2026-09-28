using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Logout;
using SOFIA.Domain.Entities;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.UnitTests.Security.Commands.Logout;

public class LogoutCommandHandlerTests
{
    private const string RawToken = "raw-refresh-token";

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Cuenta _cuenta = CuentaFactory.WithBaseBranch(Guid.NewGuid(), null);
    private readonly List<DomainRefreshToken> _tokens = [];
    private readonly LogoutCommandHandler _handler;

    public LogoutCommandHandlerTests()
    {
        _ = _dbContextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new LogoutCommandHandler(_dbContextMock.Object);
    }

    private void SetupContext()
    {
        _ = _dbContextMock.Setup(c => c.RefreshTokens).Returns(_tokens.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
    }

    private DomainRefreshToken AddToken(string rawToken)
    {
        var token = DomainRefreshToken.Create(_cuenta.Id, Hash(rawToken), DateTimeOffset.UtcNow.AddDays(7));
        _tokens.Add(token);
        return token;
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    [Fact]
    public async Task Handle_ShouldRevokeSessionFromCookie_WhenAccessTokenExpired()
    {
        var presented = AddToken(RawToken);
        var otherDevice = AddToken("other-device-token");
        SetupContext();
        var stamp = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new LogoutCommand(RefreshToken: RawToken), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = presented.IsRevoked.Should().BeTrue(because: "the cookie alone must end the session when the bearer is gone");
        _ = otherDevice.IsRevoked.Should().BeTrue();
        _ = _cuenta.SecurityStamp.Should().NotBe(stamp);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRevokeSessionFromPrincipal_WhenNoCookie()
    {
        var token = AddToken(RawToken);
        SetupContext();
        var stamp = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new LogoutCommand(_cuenta.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = token.IsRevoked.Should().BeTrue();
        _ = _cuenta.SecurityStamp.Should().NotBe(stamp);
    }

    [Fact]
    public async Task Handle_ShouldSucceedWithoutChanges_WhenNeitherPrincipalNorCookie()
    {
        var token = AddToken(RawToken);
        SetupContext();
        var stamp = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new LogoutCommand(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = token.IsRevoked.Should().BeFalse();
        _ = _cuenta.SecurityStamp.Should().Be(stamp);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSucceedWithoutChanges_WhenCookieIsUnknown()
    {
        var token = AddToken(RawToken);
        SetupContext();
        var stamp = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new LogoutCommand(RefreshToken: "unknown-token"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue(because: "logout must not reveal whether a session existed");
        _ = token.IsRevoked.Should().BeFalse();
        _ = _cuenta.SecurityStamp.Should().Be(stamp);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRevokeRotatedSession_WhenCookieWasJustRotatedByARacingRefresh()
    {
        var rotated = AddToken(RawToken);
        rotated.Revoke();
        var current = AddToken("current-token");
        SetupContext();
        var stamp = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new LogoutCommand(RefreshToken: RawToken), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = current.IsRevoked.Should().BeTrue(because: "a refresh that landed before the logout must not keep the session alive");
        _ = _cuenta.SecurityStamp.Should().NotBe(stamp);
    }

    [Fact]
    public async Task Handle_ShouldSucceedWithoutChanges_WhenCookieIsExpired()
    {
        var expired = DomainRefreshToken.Create(_cuenta.Id, Hash(RawToken), DateTimeOffset.UtcNow.AddMinutes(-1));
        _tokens.Add(expired);
        var active = AddToken("current-token");
        SetupContext();
        var stamp = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new LogoutCommand(RefreshToken: RawToken), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = active.IsRevoked.Should().BeFalse();
        _ = _cuenta.SecurityStamp.Should().Be(stamp);
    }
}
