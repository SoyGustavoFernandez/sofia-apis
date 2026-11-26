using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Onboarding.Commands.CrearUnidadesBasicas;
using SOFIA.Application.Onboarding.Commands.RegistrarPrimerProducto;
using SOFIA.Application.Onboarding.Queries.GetPrimerosPasos;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Onboarding;

public class OnboardingIntegrationTests : BaseIntegrationTest
{
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so the progress of one never leaks into another
    public OnboardingIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        CurrentUser.Admin = true;
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private async Task<(Guid SucursalId, Guid EmpleadoId)> SeedAdminEnSucursalAsync()
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;
        return (sucursalId, empleadoId);
    }

    private async Task<Guid> SeedUnidadAsync()
    {
        var unidad = UnidadMedida.Create("UND", "Unidad").Value!;
        _ = DbContext.UnidadesMedida.Add(unidad);
        _ = await DbContext.SaveChangesAsync();
        return unidad.Id;
    }

    private static RegistrarPrimerProductoCommand Producto(Guid unidadId, string codigo = "COD-1", string laboratorio = "Laboratorio Andino") => new(
        codigo, "Paracetamol 500 mg", laboratorio, unidadId, CondicionVenta.VentaLibreOTC, 1.5m, "L-001", DateTimeOffset.UtcNow.AddYears(1), 100m);

    [Fact]
    public async Task PrimerosPasos_ShouldShowEverythingPending_WhenTheCompanyIsNew()
    {
        var (sucursalId, _) = await SeedAdminEnSucursalAsync();

        var result = await Sender.Send(new GetPrimerosPasosQuery());

        _ = result.Value.Should().Be(new PrimerosPasosDto(true, sucursalId, false, false, false, false));
    }

    [Fact]
    public async Task PrimerosPasos_ShouldMarkEachStep_WhenItsDataExists()
    {
        var (sucursalId, empleadoId) = await SeedAdminEnSucursalAsync();
        _ = DbContext.SUNATSeriesFiscales.Add(SunatSerieFiscal.Create(sucursalId, TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoActiva).Value!);
        _ = await DbContext.SaveChangesAsync();
        _ = await Sender.Send(Producto(await SeedUnidadAsync()));
        _ = await _seeder.SeedSesionAbiertaAsync(sucursalId, empleadoId);
        _ = await _seeder.SeedEmpleadoAsync(sucursalId);

        var result = await Sender.Send(new GetPrimerosPasosQuery());

        _ = result.Value.Should().Be(new PrimerosPasosDto(true, sucursalId, true, true, true, true));
    }

    [Fact]
    public async Task PrimerosPasos_ShouldBeHidden_WhenTheCompanyAlreadySold()
    {
        var (sucursalId, empleadoId) = await SeedAdminEnSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Vendido");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 5m);
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lote.Id, 1m);

        var result = await Sender.Send(new GetPrimerosPasosQuery());

        _ = result.Value!.Mostrar.Should().BeFalse();
    }

    [Fact]
    public async Task PrimerosPasos_ShouldBeHidden_WhenTheUserIsNotAdmin()
    {
        _ = await SeedAdminEnSucursalAsync();
        CurrentUser.Admin = false;

        var result = await Sender.Send(new GetPrimerosPasosQuery());

        _ = result.Value.Should().Be(PrimerosPasosDto.Oculto);
    }

    [Fact]
    public async Task PrimerProducto_ShouldCreateProductLaboratoryBatchAndStock_InTheUserBranch()
    {
        var (sucursalId, _) = await SeedAdminEnSucursalAsync();

        var result = await Sender.Send(Producto(await SeedUnidadAsync()));

        _ = result.IsSuccess.Should().BeTrue();
        var producto = await DbContext.Medicamentos.SingleAsync(m => m.Id == result.Value);
        var lote = await DbContext.LotesInventario.SingleAsync(l => l.ProductoId == producto.Id);
        var stock = await DbContext.LotesEnSucursal.SingleAsync(i => i.LoteId == lote.Id);
        _ = producto.TenantId.Should().Be(CurrentUser.Empresa);
        _ = (await DbContext.Laboratorios.SingleAsync(l => l.Id == producto.LaboratorioId)).NombreCompania.Should().Be("Laboratorio Andino");
        _ = stock.SucursalId.Should().Be(sucursalId);
        _ = stock.CantidadFisica.Should().Be(100m);
    }

    [Fact]
    public async Task PrimerProducto_ShouldReuseTheLaboratory_WhenItAlreadyExists()
    {
        _ = await SeedAdminEnSucursalAsync();
        var unidadId = await SeedUnidadAsync();
        _ = await Sender.Send(Producto(unidadId));

        var result = await Sender.Send(Producto(unidadId, "COD-2", "  Laboratorio Andino "));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await DbContext.Laboratorios.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task PrimerProducto_ShouldReject_WhenTheCodeIsAlreadyUsed()
    {
        _ = await SeedAdminEnSucursalAsync();
        var unidadId = await SeedUnidadAsync();
        _ = await Sender.Send(Producto(unidadId));

        var result = await Sender.Send(Producto(unidadId));

        _ = result.Error.Code.Should().Be("Medicamento.CodigoNacional.Duplicado");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task PrimerProducto_ShouldReject_WhenTheUnitBelongsToAnotherCompany()
    {
        _ = await SeedAdminEnSucursalAsync();
        var unidadAjena = await SeedUnidadAsync();
        CurrentUser.Empresa = Guid.NewGuid();

        var result = await Sender.Send(Producto(unidadAjena));

        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
    }

    [Fact]
    public async Task Commands_ShouldBeForbidden_WhenTheUserIsNotAdmin()
    {
        _ = await SeedAdminEnSucursalAsync();
        CurrentUser.Admin = false;

        var producto = await Sender.Send(Producto(Guid.NewGuid()));
        var unidades = await Sender.Send(new CrearUnidadesBasicasCommand());

        _ = producto.StatusCode.Should().Be(403);
        _ = unidades.StatusCode.Should().Be(403);
    }

    [Fact]
    public async Task UnidadesBasicas_ShouldBeCreatedOnce_WhenTheCompanyHasNone()
    {
        var primera = await Sender.Send(new CrearUnidadesBasicasCommand());
        var segunda = await Sender.Send(new CrearUnidadesBasicasCommand());

        _ = primera.Value.Should().Be(UnidadMedida.Basicas.Count);
        _ = segunda.Value.Should().Be(0);
        _ = (await DbContext.UnidadesMedida.CountAsync()).Should().Be(UnidadMedida.Basicas.Count);
    }
}
