using Microsoft.EntityFrameworkCore;
using SOFIA.Application.FormulacionesClinicas.Commands.Create;
using SOFIA.Application.FormulacionesClinicas.Queries.GetFormulacionesClinicasWithPagination;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.FormulacionesClinicas;

public class CreateFormulacionClinicaIntegrationTests(SofiaWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateFormulacionClinica_ShouldPersistToDatabase_WhenCommandIsValid()
    {
        // Arrange — necesitamos medicamento, ingrediente y unidad de medida reales en la BD de test
        var unidadId = await SeedUnidadMedidaAsync();
        var ingredienteId = await SeedIngredienteActivoAsync("Espironolactona", "C03DA01");
        var laboratorioId = await SeedLaboratorioAsync();
        var productoId = await SeedMedicamentoAsync("Aldactone 100mg", laboratorioId, unidadId);

        var command = new CreateFormulacionClinicaCommand
        {
            ProductoId = productoId,
            IngredienteId = ingredienteId,
            ConcentracionDosis = 100m,
            UnidadMedidaId = unidadId,
            CodigoTeOrange = "TE045",
        };

        // Act
        var result = await Sender.Send(command);

        // Assert
        _ = result.IsSuccess.Should().BeTrue(because: "el command es válido y los FK existen");

        var persistida = await DbContext.FormulacionesClinicas
            .FirstOrDefaultAsync(f => f.Id == result.Value);

        _ = persistida.Should().NotBeNull();
        _ = persistida.UnidadMedidaId.Should().Be(unidadId);
    }

    [Fact]
    public async Task CreateFormulacionClinica_ShouldReturnNotFound_WhenUnidadMedidaDoesNotExist()
    {
        var ingredienteId = await SeedIngredienteActivoAsync("Ibuprofeno", "M01AE01");
        var laboratorioId = await SeedLaboratorioAsync();
        var unidadId = await SeedUnidadMedidaAsync();
        var productoId = await SeedMedicamentoAsync("Actron 400mg", laboratorioId, unidadId);

        var command = new CreateFormulacionClinicaCommand
        {
            ProductoId = productoId,
            IngredienteId = ingredienteId,
            ConcentracionDosis = 400m,
            UnidadMedidaId = Guid.NewGuid(), // no existe
        };

        var result = await Sender.Send(command);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
    }

    [Fact]
    public async Task SearchByIngredienteNombre_ShouldReturnMedicationsContainingIt()
    {
        // Escenario real: buscar "Espironolactona" debe devolver los medicamentos que la contienen.
        var unidadId = await SeedUnidadMedidaAsync();
        var ingredienteId = await SeedIngredienteActivoAsync($"Espironolactona-{Guid.NewGuid():N}", "C03DA01");
        var laboratorioId = await SeedLaboratorioAsync();
        var aldactoneId = await SeedMedicamentoAsync($"Aldactone-{Guid.NewGuid():N}", laboratorioId, unidadId);
        var diurexId = await SeedMedicamentoAsync($"Diurex-{Guid.NewGuid():N}", laboratorioId, unidadId);

        var ingredienteNombre = (await DbContext.IngredientesActivos.FindAsync(ingredienteId))!.DenominacionDci;

        _ = await Sender.Send(new CreateFormulacionClinicaCommand
        {
            ProductoId = aldactoneId,
            IngredienteId = ingredienteId,
            ConcentracionDosis = 100m,
            UnidadMedidaId = unidadId,
        });
        _ = await Sender.Send(new CreateFormulacionClinicaCommand
        {
            ProductoId = diurexId,
            IngredienteId = ingredienteId,
            ConcentracionDosis = 25m,
            UnidadMedidaId = unidadId,
        });

        var result = await Sender.Send(new GetFormulacionesClinicasWithPaginationQuery
        {
            IngredienteNombre = ingredienteNombre,
            PageNumber = 1,
            PageSize = 10,
        });

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Items.Should().HaveCount(2);
        _ = result.Value.Items.Should().Contain(f => f.ProductoId == aldactoneId && f.UnidadMedidaNombre != null);
        _ = result.Value.Items.Should().Contain(f => f.ProductoId == diurexId);
    }

    private async Task<Guid> SeedLaboratorioAsync()
    {
        var laboratorio = Domain.Entities.Laboratorio.Create(
            $"LabTest{Guid.NewGuid():N}"[..20],
            $"LAB-{Guid.NewGuid():N}"[..15]).Value!;

        _ = DbContext.Laboratorios.Add(laboratorio);
        _ = await DbContext.SaveChangesAsync();
        return laboratorio.Id;
    }

    private async Task<Guid> SeedUnidadMedidaAsync()
    {
        var unidad = Domain.Entities.UnidadMedida.Create(
            $"U{Guid.NewGuid():N}"[..10],
            "Unidad Test").Value!;

        _ = DbContext.UnidadesMedida.Add(unidad);
        _ = await DbContext.SaveChangesAsync();
        return unidad.Id;
    }

    private async Task<Guid> SeedIngredienteActivoAsync(string denominacionDci, string codigoAtc)
    {
        var ingrediente = Domain.Entities.IngredienteActivo.Create(denominacionDci, codigoAtc).Value!;

        _ = DbContext.IngredientesActivos.Add(ingrediente);
        _ = await DbContext.SaveChangesAsync();
        return ingrediente.Id;
    }

    private async Task<Guid> SeedMedicamentoAsync(string nombreComercial, Guid laboratorioId, Guid unidadBaseId)
    {
        var medicamento = Domain.Entities.Medicamento.Create(
            $"COD-{Guid.NewGuid():N}"[..20],
            nombreComercial,
            laboratorioId,
            unidadBaseId,
            CondicionVenta.VentaLibreOTC).Value!;

        _ = DbContext.Medicamentos.Add(medicamento);
        _ = await DbContext.SaveChangesAsync();
        return medicamento.Id;
    }
}
