using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
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
    public async Task Handle_ShouldRetryAndRevokeTokenIssuedMeanwhile_WhenRefreshRacesLogout()
    {
        var presented = AddToken(RawToken);
        SetupContext();
        DomainRefreshToken? issuedByRacingRefresh = null;

        _ = _dbContextMock
            .SetupSequence(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                // The racing refresh rotated the presented token and issued a new one before this save
                issuedByRacingRefresh = AddToken("issued-by-refresh");
                return Task.FromException<int>(new DbUpdateConcurrencyException("Concurrent refresh"));
            })
            .ReturnsAsync(1);

        var result = await _handler.Handle(new LogoutCommand(RefreshToken: RawToken), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue(because: "logout must never surface a concurrency conflict");
        _ = presented.IsRevoked.Should().BeTrue();
        _ = issuedByRacingRefresh!.IsRevoked.Should().BeTrue(because: "the token issued by the racing refresh must not survive the logout");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldGiveUp_AfterThreeConcurrencyConflicts()
    {
        _ = AddToken(RawToken);
        SetupContext();
        _ = _dbContextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException("Concurrent refresh"));

        var act = () => _handler.Handle(new LogoutCommand(RefreshToken: RawToken), CancellationToken.None);

        _ = await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(3));
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
