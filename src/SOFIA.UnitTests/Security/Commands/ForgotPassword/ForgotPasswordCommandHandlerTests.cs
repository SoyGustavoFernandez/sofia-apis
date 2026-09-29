using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.ForgotPassword;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.ForgotPassword;

public class ForgotPasswordCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "hash").Value!;
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new ForgotPasswordCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldStoreOnlyATokenHash_WhenAccountIsActive()
    {
        var result = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.RecoveryToken.Should().MatchRegex("^[0-9A-F]{64}$", because: "only the SHA-256 hash of the token is stored");
        _ = _cuenta.RecoveryTokenExpiry.Should().BeAfter(DateTimeOffset.UtcNow);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldIssueADifferentTokenEachTime()
    {
        _ = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);
        var first = _cuenta.RecoveryToken;

        _ = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);

        _ = _cuenta.RecoveryToken.Should().NotBe(first);
    }

    [Fact]
    public async Task Handle_ShouldIssueNoToken_ButSameResponse_WhenAccountIsInactive()
    {
        var unknownResult = await _handler.Handle(new ForgotPasswordCommand("no-existe"), CancellationToken.None);
        _cuenta.ToggleActive();

        var result = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Should().BeEquivalentTo(unknownResult, because: "the response must not reveal the account state");
        _ = _cuenta.RecoveryToken.Should().BeNull();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
