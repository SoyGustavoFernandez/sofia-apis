using Microsoft.EntityFrameworkCore;
using SOFIA.Application.IngredientesActivos.Commands.DeleteIngredienteActivo;
using SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;
using SOFIA.Application.Pacientes.Commands.DeletePaciente;
using SOFIA.Application.Profesionales.Commands.DeleteProfesionalSalud;
using SOFIA.Application.Proveedores.Commands.DeleteProveedor;
using SOFIA.Application.Sucursales.Commands.DeleteSucursal;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Maestros;

// Delete guards against real SQL Server: the in-use queries must translate, refuse the delete and leave the record visible
public class DeleteMaestroInUseIntegrationTests : BaseIntegrationTest
{
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so records seeded by other tests never count as dependents
    public DeleteMaestroInUseIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private async Task<Sucursal> SeedSucursalSinEmpleadosAsync()
    {
        var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = DbContext.Sucursales.Add(sucursal);
        _ = await DbContext.SaveChangesAsync();
        return sucursal;
    }

    private async Task<ProfesionalSalud> SeedProfesionalAsync()
    {
        var profesional = ProfesionalSalud.Create($"CMP{Guid.NewGuid():N}"[..12], "Dra. Rios", null).Value!;
        _ = DbContext.ProfesionalesSalud.Add(profesional);
        _ = await DbContext.SaveChangesAsync();
        return profesional;
    }

    private async Task<PacienteCliente> SeedPacienteAsync()
    {
        var paciente = PacienteCliente.Create(TipoDocumentoIdentidad.Dni, $"{Random.Shared.Next(10_000_000, 99_999_999)}", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
        _ = DbContext.Pacientes.Add(paciente);
        _ = await DbContext.SaveChangesAsync();
        return paciente;
    }

    private async Task<IngredienteActivo> SeedIngredienteAsync()
    {
        var ingrediente = IngredienteActivo.Create($"Ing{Guid.NewGuid():N}"[..20], "N02BE01").Value!;
        _ = DbContext.IngredientesActivos.Add(ingrediente);
        _ = await DbContext.SaveChangesAsync();
        return ingrediente;
    }

    private async Task<ProveedorDistribuidor> SeedProveedorAsync()
    {
        var proveedor = ProveedorDistribuidor.Create($"Prov{Guid.NewGuid():N}"[..20], $"20{Random.Shared.NextInt64(100_000_000, 999_999_999)}", null, null).Value!;
        _ = DbContext.Proveedores.Add(proveedor);
        _ = await DbContext.SaveChangesAsync();
        return proveedor;
    }

    private async Task<bool> IsDeletedAsync<T>(Guid id) where T : BaseEntity =>
        await DbContext.Set<T>().IgnoreQueryFilters().Where(e => e.Id == id).Select(e => e.IsDeleted).SingleAsync();

    [Fact]
    public async Task DeleteMedicamento_ShouldBeRefused_WhenItHasALote()
    {
        var (sucursalId, _) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto en uso");
        _ = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 5m);

        var result = await Sender.Send(new DeleteMedicamentoCommand(medicamento.Id));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.InUse");
        _ = result.StatusCode.Should().Be(409);
        _ = (await IsDeletedAsync<Medicamento>(medicamento.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteMedicamento_ShouldSoftDelete_WhenNothingReferencesIt()
    {
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto libre");

        var result = await Sender.Send(new DeleteMedicamentoCommand(medicamento.Id));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await IsDeletedAsync<Medicamento>(medicamento.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteSucursal_ShouldBeRefused_WhenItHasEmpleados()
    {
        var (sucursalId, _) = await _seeder.SeedSucursalAsync();

        var result = await Sender.Send(new DeleteSucursalCommand(sucursalId));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.InUse");
        _ = result.StatusCode.Should().Be(409);
        _ = (await IsDeletedAsync<Sucursal>(sucursalId)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSucursal_ShouldBeRefused_WhenAnAccountIsAssignedToIt()
    {
        var (_, empleadoId) = await _seeder.SeedSucursalAsync();
        var sucursal = await SeedSucursalSinEmpleadosAsync();
        var cuenta = Cuenta.Create(empleadoId, $"user{Guid.NewGuid():N}"[..20], "hash").Value!;
        cuenta.AddSucursal(sucursal);
        _ = DbContext.Cuentas.Add(cuenta);
        _ = await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new DeleteSucursalCommand(sucursal.Id));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.InUse");
        _ = (await IsDeletedAsync<Sucursal>(sucursal.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSucursal_ShouldBeRefused_WhenARoleIsAssignedToIt()
    {
        var sucursal = await SeedSucursalSinEmpleadosAsync();
        var rol = Rol.Create($"Rol{Guid.NewGuid():N}"[..20], null).Value!;
        rol.AddSucursal(sucursal);
        _ = DbContext.Roles.Add(rol);
        _ = await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new DeleteSucursalCommand(sucursal.Id));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.InUse");
        _ = (await IsDeletedAsync<Sucursal>(sucursal.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteSucursal_ShouldSoftDelete_WhenNothingReferencesIt()
    {
        var sucursal = await SeedSucursalSinEmpleadosAsync();

        var result = await Sender.Send(new DeleteSucursalCommand(sucursal.Id));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await IsDeletedAsync<Sucursal>(sucursal.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DeletePaciente_ShouldBeRefused_WhenItHasARecetaOrAVenta()
    {
        var paciente = await SeedPacienteAsync();
        var profesional = await SeedProfesionalAsync();
        _ = DbContext.Recetas.Add(RecetaMedica.Create(paciente.Id, profesional.Id, new DateOnly(2026, 1, 1)).Value!);
        _ = await DbContext.SaveChangesAsync();

        var porReceta = await Sender.Send(new DeletePacienteCommand(paciente.Id));

        var otro = await SeedPacienteAsync();
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto venta paciente");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 5m);
        _ = DbContext.Ventas.Add(Venta.Create(sucursalId, empleadoId, otro.Id, null, [DetalleVenta.Create(lote.Id, 1m, 10m, 8m).Value!]).Value!);
        _ = await DbContext.SaveChangesAsync();

        var porVenta = await Sender.Send(new DeletePacienteCommand(otro.Id));

        _ = porReceta.IsFailure.Should().BeTrue();
        _ = porReceta.Error.Code.Should().Be("Paciente.InUse");
        _ = porReceta.StatusCode.Should().Be(409);
        _ = porVenta.IsFailure.Should().BeTrue();
        _ = porVenta.Error.Code.Should().Be("Paciente.InUse");
        _ = (await IsDeletedAsync<PacienteCliente>(paciente.Id)).Should().BeFalse();
        _ = (await IsDeletedAsync<PacienteCliente>(otro.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeletePaciente_ShouldSoftDelete_WhenNothingReferencesIt()
    {
        var paciente = await SeedPacienteAsync();

        var result = await Sender.Send(new DeletePacienteCommand(paciente.Id));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await IsDeletedAsync<PacienteCliente>(paciente.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProfesionalSalud_ShouldBeRefused_WhenItHasARecetas()
    {
        var paciente = await SeedPacienteAsync();
        var profesional = await SeedProfesionalAsync();
        _ = DbContext.Recetas.Add(RecetaMedica.Create(paciente.Id, profesional.Id, new DateOnly(2026, 1, 1)).Value!);
        _ = await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new DeleteProfesionalSaludCommand(profesional.Id));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ProfesionalSalud.InUse");
        _ = result.StatusCode.Should().Be(409);
        _ = (await IsDeletedAsync<ProfesionalSalud>(profesional.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProfesionalSalud_ShouldSoftDelete_WhenNothingReferencesIt()
    {
        var profesional = await SeedProfesionalAsync();

        var result = await Sender.Send(new DeleteProfesionalSaludCommand(profesional.Id));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await IsDeletedAsync<ProfesionalSalud>(profesional.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteIngredienteActivo_ShouldBeRefused_WhenItHasAFormulacion()
    {
        var ingrediente = await SeedIngredienteAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto formulado");
        _ = DbContext.FormulacionesClinicas.Add(FormulacionClinica.Create(medicamento.Id, ingrediente.Id, 500m, medicamento.UnidadBaseId, null).Value!);
        _ = await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new DeleteIngredienteActivoCommand(ingrediente.Id));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("IngredienteActivo.InUse");
        _ = result.StatusCode.Should().Be(409);
        _ = (await IsDeletedAsync<IngredienteActivo>(ingrediente.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteIngredienteActivo_ShouldSoftDelete_WhenNothingReferencesIt()
    {
        var ingrediente = await SeedIngredienteAsync();

        var result = await Sender.Send(new DeleteIngredienteActivoCommand(ingrediente.Id));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await IsDeletedAsync<IngredienteActivo>(ingrediente.Id)).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteProveedor_ShouldBeRefused_WhenItHasPrecios()
    {
        var proveedor = await SeedProveedorAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto cotizado");
        _ = DbContext.HistorialPreciosProveedor.Add(HistorialPrecioProveedor.Create(proveedor.Id, medicamento.Id, 5m, DateTime.UtcNow, null, 3).Value!);
        _ = await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new DeleteProveedorCommand(proveedor.Id));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Proveedor.InUse");
        _ = result.StatusCode.Should().Be(409);
        _ = (await IsDeletedAsync<ProveedorDistribuidor>(proveedor.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteProveedor_ShouldSoftDelete_WhenNothingReferencesIt()
    {
        var proveedor = await SeedProveedorAsync();

        var result = await Sender.Send(new DeleteProveedorCommand(proveedor.Id));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await IsDeletedAsync<ProveedorDistribuidor>(proveedor.Id)).Should().BeTrue();
    }
}
