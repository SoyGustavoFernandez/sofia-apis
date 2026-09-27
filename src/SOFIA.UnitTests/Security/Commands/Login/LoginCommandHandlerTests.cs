using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Login;
using SOFIA.Domain.Entities;
using DomainRefreshToken = SOFIA.Domain.Entities.RefreshToken;

namespace SOFIA.UnitTests.Security.Commands.Login;

public class LoginCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private static readonly Guid TenantId = Guid.NewGuid();
    private readonly Cuenta _cuenta = CuentaFactory.WithBaseBranch(TenantId, TenantId, passwordHash: "real_hash");
    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.RefreshTokens).Returns(new List<DomainRefreshToken>().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns("dummy_hash");
        _ = _jwtProviderMock
            .Setup(j => j.Generate(It.IsAny<Cuenta>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .Returns("fake.jwt.token");

        _handler = new LoginCommandHandler(
            _dbContextMock.Object,
            _passwordHasherMock.Object,
            _jwtProviderMock.Object,
            _currentUserMock.Object,
            NullLogger<LoginCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_ValidCredentials_ReturnsTokens()
    {
        _ = _passwordHasherMock.Setup(p => p.Verify("Clave123", "real_hash")).Returns(true);

        var result = await _handler.Handle(new LoginCommand("usuario", "Clave123"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.AccessToken.Should().Be("fake.jwt.token");
    }

    [Fact]
    public async Task Handle_LockedAccount_ReturnsSameErrorAsWrongPassword()
    {
        for (var i = 0; i < 5; i++)
        {
            _cuenta.RegisterFailedAttempt();
        }

        _ = _passwordHasherMock.Setup(p => p.Verify("Clave123", "real_hash")).Returns(true);
        var locked = await _handler.Handle(new LoginCommand("usuario", "Clave123"), CancellationToken.None);
        var unknown = await _handler.Handle(new LoginCommand("no-existe", "Clave123"), CancellationToken.None);

        _ = locked.IsFailure.Should().BeTrue();
        _ = locked.Error.Should().Be(unknown.Error, because: "the lock state must not confirm that the account exists");
        _ = locked.StatusCode.Should().Be(unknown.StatusCode);
    }

    [Fact]
    public async Task Handle_BaseBranchOfAnotherTenant_ReturnsSameErrorAsWrongPasswordAndIssuesNoToken()
    {
        var cuenta = CuentaFactory.WithBaseBranch(TenantId, Guid.NewGuid(), "intruso", "intruso_hash");
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { cuenta }.BuildMockDbSet().Object);
        _ = _passwordHasherMock.Setup(p => p.Verify("Clave123", "intruso_hash")).Returns(true);

        var result = await _handler.Handle(new LoginCommand("intruso", "Clave123"), CancellationToken.None);
        var unknown = await _handler.Handle(new LoginCommand("no-existe", "Clave123"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Should().Be(unknown.Error, because: "the rejection reason must not leak");
        _ = result.StatusCode.Should().Be(401);
        _jwtProviderMock.Verify(j => j.Generate(It.IsAny<Cuenta>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnknownUsername_StillVerifiesPassword()
    {
        var result = await _handler.Handle(new LoginCommand("no-existe", "Clave123"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _passwordHasherMock.Verify(p => p.Verify("Clave123", It.IsAny<string>()), Times.Once, "skipping BCrypt would leak account existence through timing");
    }
}
