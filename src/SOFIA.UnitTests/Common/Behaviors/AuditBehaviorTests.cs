using System.Reflection;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Behaviors;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Empresas.Commands.UpdateEmpresa;
using SOFIA.Application.Inventarios.Commands.RegisterInventario;
using SOFIA.Application.POS.Commands.CerrarCaja;
using SOFIA.Application.Security.Commands.Register;
using SOFIA.Application.Security.Commands.Roles.CreateRol;
using SOFIA.Application.Security.Commands.Roles.RevokePermission;
using SOFIA.Application.Ventas.Commands.AnularVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Common.Behaviors;

public class AuditBehaviorTests
{
    private readonly Guid _empleadoId = Guid.NewGuid();
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly List<AuditoriaEventoSeguridad> _eventos = [];

    public AuditBehaviorTests()
    {
        var auditoriaMock = _eventos.BuildMockDbSet();
        _ = auditoriaMock.Setup(s => s.Add(It.IsAny<AuditoriaEventoSeguridad>())).Callback<AuditoriaEventoSeguridad>(_eventos.Add);
        _ = _dbContextMock.Setup(c => c.AuditoriasEventosSeguridad).Returns(auditoriaMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.Id).Returns(_empleadoId.ToString());
        _ = _currentUserMock.Setup(u => u.ClientIpAddress).Returns("10.0.0.7");
    }

    private Task<TResponse> Run<TRequest, TResponse>(TRequest request, TResponse response)
        where TRequest : MediatR.IRequest<TResponse> =>
        new AuditBehavior<TRequest, TResponse>(_dbContextMock.Object, _currentUserMock.Object)
            .Handle(request, _ => Task.FromResult(response), CancellationToken.None);

    [Fact]
    public async Task Handle_AuditableCommandSucceeds_WritesExactlyOneEvent()
    {
        var command = new CerrarCajaCommand(Guid.NewGuid(), 150.5m);

        _ = await Run(command, Result.Success(command.SesionId));

        _ = _eventos.Should().ContainSingle();
        var evento = _eventos[0];
        _ = evento.TipoAccion.Should().Be(AuditEventos.CajaCerrar);
        _ = evento.TablaAfectada.Should().Be(AuditTablas.SesionesCaja);
        _ = evento.RegistroIdAfectado.Should().Be(command.SesionId);
        _ = evento.EmpleadoId.Should().Be(_empleadoId);
        _ = evento.DireccionIp.Should().Be("10.0.0.7");
        _ = evento.PayloadNuevo.Should().Contain("declarado").And.Contain("150.5");
        _ = evento.FechaHoraEvento.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_AuditableCommandFails_WritesNoEvent()
    {
        _ = await Run(new AnularVentaCommand(Guid.NewGuid(), "Error"), Result.Failure(Error.NotFound("Venta.NotFound", "x")));

        _ = _eventos.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidationFails_HandlerAndAuditNeverRun()
    {
        var command = new AnularVentaCommand(Guid.NewGuid(), string.Empty);
        var validation = new ValidationBehavior<AnularVentaCommand, Result>([new AnularVentaCommandValidator()]);
        var audit = new AuditBehavior<AnularVentaCommand, Result>(_dbContextMock.Object, _currentUserMock.Object);

        var result = await validation.Handle(command, ct => audit.Handle(command, _ => Task.FromResult(Result.Success()), ct), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = _eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonAuditableCommand_WritesNoEvent()
    {
        _ = await Run(new NotAuditedCommand(), Result.Success());

        _ = _eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NoAuthenticatedEmployee_WritesNoEvent()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(false);

        _ = await Run(new RevokePermissionFromRolCommand(Guid.NewGuid()), Result.Success());

        _ = _eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_CreateCommand_UsesReturnedIdAsAffectedRecord()
    {
        var rolId = Guid.NewGuid();

        _ = await Run(new CreateRolCommand("Cajero", null, 3), Result.Success(rolId));

        _ = _eventos.Should().ContainSingle(e => e.TipoAccion == AuditEventos.RolCrear && e.RegistroIdAfectado == rolId && e.TablaAfectada == AuditTablas.Roles);
    }

    [Fact]
    public async Task Handle_RevokePermission_RecordsPermissionAsAffectedRecord()
    {
        var permisoId = Guid.NewGuid();

        _ = await Run(new RevokePermissionFromRolCommand(permisoId), Result.Success());

        _ = _eventos.Should().ContainSingle(e => e.TipoAccion == AuditEventos.PermisoRevocar && e.RegistroIdAfectado == permisoId);
    }

    [Fact]
    public async Task Handle_RegisterInventarioWithoutDirectAdjust_WritesNoEvent()
    {
        _ = await Run(new RegisterInventarioCommand(Guid.NewGuid(), Guid.NewGuid(), 5m), Result.Success(Guid.NewGuid()));

        _ = _eventos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_RegisterInventarioDirectAdjust_WritesStockAdjustEvent()
    {
        var inventarioId = Guid.NewGuid();

        _ = await Run(new RegisterInventarioCommand(Guid.NewGuid(), Guid.NewGuid(), 5m, EsAjusteDirecto: true), Result.Success(inventarioId));

        _ = _eventos.Should().ContainSingle(e => e.TipoAccion == AuditEventos.StockAjustar && e.RegistroIdAfectado == inventarioId);
    }

    [Fact]
    public async Task Handle_RegisterAccount_DetailContainsNoUsernameOrPassword()
    {
        var command = new RegisterAccountCommand(Guid.NewGuid(), "jperez.quispe", "Secreta#2026");

        _ = await Run(command, Result.Success(Guid.NewGuid()));

        var detalle = _eventos.Should().ContainSingle().Which.PayloadNuevo;
        _ = detalle.Should().NotContain("jperez").And.NotContain("Secreta");
    }

    [Fact]
    public async Task Handle_UpdateEmpresa_DetailContainsNoRucOrName()
    {
        var command = new UpdateEmpresaCommand { Id = Guid.NewGuid(), Nombre = "Botica Juan Perez", RUC = "10456789012" };

        _ = await Run(command, Result.Success());

        var evento = _eventos.Should().ContainSingle().Which;
        _ = evento.RegistroIdAfectado.Should().Be(command.Id);
        _ = (evento.PayloadNuevo ?? string.Empty).Should().NotContain("45678901").And.NotContain("Perez");
    }

    [Fact]
    public void AuditConstants_FitTheirColumns()
    {
        static IEnumerable<string> Values(Type t)
        {
            return t.GetFields(BindingFlags.Public | BindingFlags.Static).Select(f => (string)f.GetRawConstantValue()!);
        }

        _ = Values(typeof(AuditEventos)).Should().OnlyContain(v => v.Length <= 20);
        _ = Values(typeof(AuditTablas)).Should().OnlyContain(v => v.Length <= 50);
        _ = Values(typeof(AuditEventos)).Should().OnlyHaveUniqueItems();
    }

    private sealed record NotAuditedCommand : ICommand;
}
