using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.RefreshToken;
using SOFIA.Domain.Entities;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.UnitTests.Security.Commands.RefreshToken;

public class RefreshTokenCommandHandlerTests
{
    private const string RawToken = "raw-refresh-token";

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "hash").Value!;
    private readonly List<DomainRefreshToken> _tokens = [];
    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _ = _jwtProviderMock
            .Setup(j => j.Generate(It.IsAny<Cuenta>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .Returns("fake.jwt.token");
        _ = _dbContextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new RefreshTokenCommandHandler(
            _dbContextMock.Object,
            _jwtProviderMock.Object,
            NullLogger<RefreshTokenCommandHandler>.Instance);
    }

    private void SetupContext()
    {
        var tokensDbSet = _tokens.BuildMockDbSet();
        _ = tokensDbSet.Setup(d => d.Add(It.IsAny<DomainRefreshToken>()));
        _ = _dbContextMock.Setup(c => c.RefreshTokens).Returns(tokensDbSet.Object);

        var cuentasDbSet = new List<Cuenta> { _cuenta }.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(cuentasDbSet.Object);
    }

    private DomainRefreshToken AddToken(string rawToken, DateTimeOffset? expiresAt = null)
    {
        var token = DomainRefreshToken.Create(_cuenta.Id, Hash(rawToken), expiresAt ?? DateTimeOffset.UtcNow.AddDays(7));
        _tokens.Add(token);
        return token;
    }

    private static void RevokeAt(DomainRefreshToken token, DateTimeOffset revokedAt)
    {
        token.Revoke();
        typeof(DomainRefreshToken).GetProperty(nameof(DomainRefreshToken.RevokedAt))!.SetValue(token, revokedAt);
    }

    private static string Hash(string rawToken) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawToken)));

    [Fact]
    public async Task Handle_ShouldRotateToken_WhenTokenIsActive()
    {
        var stored = AddToken(RawToken);
        SetupContext();

        var result = await _handler.Handle(new RefreshTokenCommand(RawToken), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.RefreshToken.Should().NotBe(RawToken);
        _ = stored.IsRevoked.Should().BeTrue(because: "the presented token must be rotated out");
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenIsExpired()
    {
        _ = AddToken(RawToken, DateTimeOffset.UtcNow.AddMinutes(-1));
        SetupContext();

        var result = await _handler.Handle(new RefreshTokenCommand(RawToken), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(401);
    }

    [Fact]
    public async Task Handle_ShouldRevokeAllSessions_WhenRotatedTokenIsReplayedAfterGracePeriod()
    {
        var replayed = AddToken(RawToken);
        RevokeAt(replayed, DateTimeOffset.UtcNow.AddMinutes(-5));
        var currentSession = AddToken("current-session-token");
        var stampBefore = _cuenta.SecurityStamp;
        SetupContext();

        var result = await _handler.Handle(new RefreshTokenCommand(RawToken), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(401);
        _ = currentSession.IsRevoked.Should().BeTrue(because: "a replayed token means the cookie was stolen");
        _ = _cuenta.SecurityStamp.Should().NotBe(stampBefore, because: "live access tokens must stop working too");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepOtherSessions_WhenRotatedTokenIsReplayedWithinGracePeriod()
    {
        var replayed = AddToken(RawToken);
        replayed.Revoke();
        var currentSession = AddToken("current-session-token");
        var stampBefore = _cuenta.SecurityStamp;
        SetupContext();

        var result = await _handler.Handle(new RefreshTokenCommand(RawToken), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(401);
        _ = currentSession.IsRevoked.Should().BeFalse(because: "two tabs refreshing at once is not theft");
        _ = _cuenta.SecurityStamp.Should().Be(stampBefore);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenTokenIsUnknown()
    {
        SetupContext();

        var result = await _handler.Handle(new RefreshTokenCommand(RawToken), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(401);
    }
}
