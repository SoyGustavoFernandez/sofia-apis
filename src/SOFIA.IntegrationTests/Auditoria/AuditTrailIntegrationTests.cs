using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Models;
using SOFIA.Application.POS.Commands.CerrarCaja;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Auditoria;

public class AuditTrailIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CerrarCaja_ShouldCommitOneAuditRowWithTheChange_WhenItSucceeds()
    {
        // Arrange
        var (sucursalId, empleadoId) = await SeedSucursalEmpleadoAsync();
        var sesion = PosSesionCaja.Create(sucursalId, empleadoId, DateTime.UtcNow, 100m).Value!;
        _ = DbContext.POSSesionesCaja.Add(sesion);
        _ = await DbContext.SaveChangesAsync();
        CurrentUser.Empleado = empleadoId;

        // Act
        var result = await Sender.Send(new CerrarCajaCommand(sesion.Id, 100m));

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        var eventos = await DbContext.AuditoriasEventosSeguridad.AsNoTracking().Where(e => e.RegistroIdAfectado == sesion.Id).ToListAsync();
        var evento = eventos.Should().ContainSingle().Which;
        _ = evento.TipoAccion.Should().Be(AuditEventos.CajaCerrar);
        _ = evento.TablaAfectada.Should().Be(AuditTablas.SesionesCaja);
        _ = evento.EmpleadoId.Should().Be(empleadoId);
        _ = evento.TenantId.Should().Be(TestCurrentUser.DefaultEmpresaId);
    }

    [Fact]
    public async Task CerrarCaja_ShouldWriteNoAuditRow_WhenTheCommandFails()
    {
        // Arrange
        var (_, empleadoId) = await SeedSucursalEmpleadoAsync();
        CurrentUser.Empleado = empleadoId;
        var sesionInexistente = Guid.NewGuid();

        // Act
        var result = await Sender.Send(new CerrarCajaCommand(sesionInexistente, 100m));

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = (await DbContext.AuditoriasEventosSeguridad.AsNoTracking().AnyAsync(e => e.RegistroIdAfectado == sesionInexistente)).Should().BeFalse();
    }

    [Fact]
    public async Task CerrarCaja_ShouldRollBackTheChange_WhenTheAuditRowCannotBeWritten()
    {
        // Arrange: an admin whose employee id does not exist makes the audit insert violate its FK
        var (sucursalId, empleadoId) = await SeedSucursalEmpleadoAsync();
        var sesion = PosSesionCaja.Create(sucursalId, empleadoId, DateTime.UtcNow, 100m).Value!;
        _ = DbContext.POSSesionesCaja.Add(sesion);
        _ = await DbContext.SaveChangesAsync();
        CurrentUser.Empleado = Guid.NewGuid();
        CurrentUser.Admin = true;

        // Act
        var act = () => Sender.Send(new CerrarCajaCommand(sesion.Id, 100m));

        // Assert
        _ = await act.Should().ThrowAsync<DbUpdateException>();
        var persisted = await DbContext.POSSesionesCaja.AsNoTracking().SingleAsync(s => s.Id == sesion.Id);
        _ = persisted.EstadoSesion.Should().Be(EstadoSesion.Abierta, because: "the close and its audit row share one transaction");
    }

    private async Task<(Guid SucursalId, Guid EmpleadoId)> SeedSucursalEmpleadoAsync()
    {
        var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = DbContext.Sucursales.Add(sucursal);
        _ = await DbContext.SaveChangesAsync();

        var empleado = Empleado.Create(sucursal.Id, "Ana", "Perez", "Gomez").Value!;
        _ = DbContext.Empleados.Add(empleado);
        _ = await DbContext.SaveChangesAsync();

        return (sucursal.Id, empleado.Id);
    }
}
