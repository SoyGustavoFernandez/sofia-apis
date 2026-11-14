using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Empresas.Commands.ActivarSuscripcionEmpresa;
using SOFIA.Application.Empresas.Commands.CancelarEmpresa;
using SOFIA.Application.Empresas.Commands.ExtenderPruebaEmpresa;
using SOFIA.Application.Empresas.Commands.SuspenderEmpresa;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.UnitTests.Security;

namespace SOFIA.UnitTests.Empresas.Commands.Suscripcion;

public class SuscripcionEmpresaCommandHandlerTests
{
    private static readonly Guid OperadorEmpresaId = Guid.NewGuid();
    private static readonly Guid ClienteEmpresaId = Guid.NewGuid();

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Empresa _cliente = EmpresaFactory.WithId(ClienteEmpresaId);
    private readonly Empresa _operador = EmpresaFactory.WithId(OperadorEmpresaId);

    public SuscripcionEmpresaCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.EsAdminPlataforma).Returns(true);
        _ = _currentUserMock.Setup(u => u.EmpresaId).Returns(OperadorEmpresaId.ToString());
        _ = _dbContextMock.Setup(c => c.Empresas).Returns(new List<Empresa> { _cliente, _operador }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
    }

    private static DateTimeOffset EnDias(int dias) => DateTimeOffset.UtcNow.AddDays(dias);

    // One entry per command so the shared guards are proven for every handler
    private Task<Result> Enviar(string comando, Guid empresaId) => comando switch
    {
        "activar" => new ActivarSuscripcionEmpresaCommandHandler(_dbContextMock.Object, _currentUserMock.Object)
            .Handle(new ActivarSuscripcionEmpresaCommand { Id = empresaId, FechaVencimiento = EnDias(30) }, CancellationToken.None),
        "extender" => new ExtenderPruebaEmpresaCommandHandler(_dbContextMock.Object, _currentUserMock.Object)
            .Handle(new ExtenderPruebaEmpresaCommand { Id = empresaId, FechaVencimiento = EnDias(45) }, CancellationToken.None),
        "suspender" => new SuspenderEmpresaCommandHandler(_dbContextMock.Object, _currentUserMock.Object)
            .Handle(new SuspenderEmpresaCommand(empresaId), CancellationToken.None),
        "cancelar" => new CancelarEmpresaCommandHandler(_dbContextMock.Object, _currentUserMock.Object)
            .Handle(new CancelarEmpresaCommand(empresaId), CancellationToken.None),
        _ => throw new ArgumentOutOfRangeException(nameof(comando))
    };

    [Theory]
    [InlineData("activar")]
    [InlineData("extender")]
    [InlineData("suspender")]
    [InlineData("cancelar")]
    public async Task Handle_ShouldReturnForbiddenAndChangeNothing_WhenCallerIsNotPlatformAdmin(string comando)
    {
        _ = _currentUserMock.Setup(u => u.EsAdminPlataforma).Returns(false);
        var fechaAntes = _cliente.FechaVencimiento;

        var result = await Enviar(comando, ClienteEmpresaId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(403);
        _ = result.Error.Code.Should().Be("Auth.PlataformaAdminRequerido");
        _ = _cliente.Estado.Should().Be(EstadoEmpresa.TrialActivo);
        _ = _cliente.FechaVencimiento.Should().Be(fechaAntes);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("activar")]
    [InlineData("extender")]
    [InlineData("suspender")]
    [InlineData("cancelar")]
    public async Task Handle_ShouldReturnNotFound_WhenEmpresaDoesNotExist(string comando)
    {
        var result = await Enviar(comando, Guid.NewGuid());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("Empresa.NotFound");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("suspender")]
    [InlineData("cancelar")]
    public async Task Handle_ShouldReturnConflict_WhenBlockingTheOperatorsOwnCompany(string comando)
    {
        var result = await Enviar(comando, OperadorEmpresaId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(409);
        _ = result.Error.Code.Should().Be("Empresa.Suscripcion.PropiaEmpresa");
        _ = _operador.EstaVigente.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Activar_ShouldActivateAnotherCompanyUntilTheGivenDate()
    {
        var vence = EnDias(30);
        var handler = new ActivarSuscripcionEmpresaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new ActivarSuscripcionEmpresaCommand { Id = ClienteEmpresaId, FechaVencimiento = vence }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cliente.Estado.Should().Be(EstadoEmpresa.Activo);
        _ = _cliente.FechaVencimiento.Should().Be(vence);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Activar_ShouldAllowTheOperatorsOwnCompany()
    {
        var result = await Enviar("activar", OperadorEmpresaId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _operador.Estado.Should().Be(EstadoEmpresa.Activo);
    }

    [Fact]
    public async Task Activar_ShouldFailAndSaveNothing_WhenDateIsNotInTheFuture()
    {
        var handler = new ActivarSuscripcionEmpresaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new ActivarSuscripcionEmpresaCommand { Id = ClienteEmpresaId, FechaVencimiento = EnDias(-1) }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empresa.Suscripcion.FechaVencimiento");
        _ = _cliente.Estado.Should().Be(EstadoEmpresa.TrialActivo);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExtenderPrueba_ShouldMoveTheExpiryAndKeepTheTrialState()
    {
        var vence = EnDias(45);
        var handler = new ExtenderPruebaEmpresaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new ExtenderPruebaEmpresaCommand { Id = ClienteEmpresaId, FechaVencimiento = vence }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cliente.Estado.Should().Be(EstadoEmpresa.TrialActivo);
        _ = _cliente.FechaVencimiento.Should().Be(vence);
    }

    [Fact]
    public async Task ExtenderPrueba_ShouldReturnConflict_WhenCompanyIsNotOnTrial()
    {
        _ = _cliente.ActivarSuscripcion(EnDias(30));

        var result = await Enviar("extender", ClienteEmpresaId);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(409);
        _ = result.Error.Code.Should().Be("Empresa.Suscripcion.NoEstaEnPrueba");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Suspender_ShouldBlockAnotherCompany()
    {
        var result = await Enviar("suspender", ClienteEmpresaId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cliente.Estado.Should().Be(EstadoEmpresa.Suspendido);
        _ = _cliente.EstaVigente.Should().BeFalse();
    }

    [Fact]
    public async Task Cancelar_ShouldBlockAnotherCompany()
    {
        var result = await Enviar("cancelar", ClienteEmpresaId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cliente.Estado.Should().Be(EstadoEmpresa.Cancelado);
        _ = _cliente.EstaVigente.Should().BeFalse();
    }

    [Fact]
    public void GetAuditEntry_ShouldNameTheEventAndTheTargetCompany()
    {
        var vence = new DateTimeOffset(2027, 1, 15, 4, 59, 59, TimeSpan.Zero);

        _ = new ActivarSuscripcionEmpresaCommand { Id = ClienteEmpresaId, FechaVencimiento = vence }.GetAuditEntry(null)
            .Should().Be(new AuditEntry(AuditEventos.EmpresaActivar, AuditTablas.Empresas, ClienteEmpresaId, "Vence: 2027-01-15"));
        _ = new ExtenderPruebaEmpresaCommand { Id = ClienteEmpresaId, FechaVencimiento = vence }.GetAuditEntry(null)
            .Should().Be(new AuditEntry(AuditEventos.EmpresaExtenderPrueba, AuditTablas.Empresas, ClienteEmpresaId, "Vence: 2027-01-15"));
        _ = new SuspenderEmpresaCommand(ClienteEmpresaId).GetAuditEntry(null)
            .Should().Be(new AuditEntry(AuditEventos.EmpresaSuspender, AuditTablas.Empresas, ClienteEmpresaId));
        _ = new CancelarEmpresaCommand(ClienteEmpresaId).GetAuditEntry(null)
            .Should().Be(new AuditEntry(AuditEventos.EmpresaCancelar, AuditTablas.Empresas, ClienteEmpresaId));
    }
}
