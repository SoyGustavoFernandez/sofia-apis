using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security.Commands.ForgotPassword;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.ForgotPassword;

public class ForgotPasswordCommandHandlerTests
{
    private static readonly Guid TenantId = Guid.NewGuid();

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IEmailSender> _emailSenderMock = new();
    private readonly Mock<IFrontendLinks> _frontendLinksMock = new();
    private readonly Cuenta _cuenta = CuentaFactory.WithBaseBranch(TenantId, TenantId, email: "ana@farmacia.pe");
    private readonly List<EmailMessage> _sent = [];
    private readonly ForgotPasswordCommandHandler _handler;

    public ForgotPasswordCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Empresas).Returns(new List<Empresa> { EmpresaFactory.WithId(TenantId) }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _emailSenderMock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .Callback<EmailMessage, CancellationToken>((m, _) => _sent.Add(m))
            .ReturnsAsync(Result.Success());
        _ = _frontendLinksMock.Setup(l => l.PasswordReset(It.IsAny<string>(), It.IsAny<string>()))
            .Returns<string, string>((token, usuario) => $"https://app.sofia.test/auth/reset-password#token={token}&usuario={usuario}");
        _handler = new ForgotPasswordCommandHandler(_dbContextMock.Object, _emailSenderMock.Object, _frontendLinksMock.Object, NullLogger<ForgotPasswordCommandHandler>.Instance);
    }

    private static string Hash(string raw) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(raw)));

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
    public async Task Handle_ShouldEmailTheEmployeeAFragmentLinkWithTheRawToken_WhenAccountIsActive()
    {
        _ = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);

        var message = _sent.Should().ContainSingle().Subject;
        _ = message.To.Should().Be("ana@farmacia.pe");
        var rawToken = message.TextBody.Split("#token=")[1].Split('&')[0];
        _ = Hash(rawToken).Should().Be(_cuenta.RecoveryToken, because: "the emailed token is the one whose hash was stored");
        _ = message.TextBody.Should().Contain($"https://app.sofia.test/auth/reset-password#token={rawToken}&usuario=usuario");
        _ = message.HtmlBody.Should().Contain($"https://app.sofia.test/auth/reset-password#token={rawToken}&amp;usuario=usuario");
        _ = message.TextBody.Should().Contain("1 hora").And.Contain("Si no lo solicitaste, ignora este correo");
        _frontendLinksMock.Verify(l => l.PasswordReset(rawToken, "usuario"), Times.Once);
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
        _ = _sent.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldIssueNoToken_WhenTheEmployeeHasNoEmail()
    {
        var sinCorreo = CuentaFactory.WithBaseBranch(TenantId, TenantId, usuario: "sincorreo");
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { sinCorreo }.BuildMockDbSet().Object);

        var result = await _handler.Handle(new ForgotPasswordCommand("sincorreo"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = sinCorreo.RecoveryToken.Should().BeNull();
        _ = _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldIssueNoToken_WhenTheEmployeeIsDeleted()
    {
        _cuenta.Empleado!.IsDeleted = true;

        var result = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.RecoveryToken.Should().BeNull();
        _ = _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldIssueNoToken_WhenTheCompanyIsNotVigente()
    {
        _ = _dbContextMock.Setup(c => c.Empresas).Returns(new List<Empresa>().BuildMockDbSet().Object);

        var result = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.RecoveryToken.Should().BeNull();
        _ = _sent.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldStillReturnGenericSuccess_WhenTheEmailCannotBeSent()
    {
        _ = _emailSenderMock.Setup(s => s.SendAsync(It.IsAny<EmailMessage>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure(Error.Failure("Email.SendFailed", "The email could not be sent.")));

        var result = await _handler.Handle(new ForgotPasswordCommand("usuario"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue(because: "a delivery failure must not reveal that the account exists");
        _ = _cuenta.RecoveryToken.Should().NotBeNull();
    }
}
