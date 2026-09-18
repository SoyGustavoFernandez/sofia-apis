using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Medicamentos.Commands.CreateMedicamento;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Medicamentos;

public class CreateMedicamentoIntegrationTests(SofiaWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateMedicamento_ShouldPersistToDatabase_WhenCommandIsValid()
    {
        // Arrange — necesitamos un laboratorio y unidad de medida reales en la BD de test
        var laboratorioId = await SeedLaboratorioAsync();
        var unidadId = await SeedUnidadMedidaAsync();

        var command = new CreateMedicamentoCommand(
            CodigoNacional: $"TEST-{Guid.NewGuid():N}"[..20],
            NombreComercial: "Paracetamol 500mg Test",
            LaboratorioId: laboratorioId,
            UnidadBaseId: unidadId,
            CondicionVenta: CondicionVenta.VentaLibreOTC,
            PrecioVentaBase: 15m);

        // Act
        var result = await Sender.Send(command);

        // Assert
        _ = result.IsSuccess.Should().BeTrue(because: "el command es válido y los FK existen");
        _ = result.Value.Should().NotBeEmpty();

        var medicamentoPersistido = await DbContext.Medicamentos
            .FirstOrDefaultAsync(m => m.Id == result.Value);

        _ = medicamentoPersistido.Should().NotBeNull();
        _ = medicamentoPersistido.NombreComercial.Should().Be("Paracetamol 500mg Test");
    }

    [Fact]
    public async Task CreateMedicamento_ShouldReturnValidationError_WhenCodigoNacionalIsEmpty()
    {
        // Arrange
        var command = new CreateMedicamentoCommand(
            CodigoNacional: "",
            NombreComercial: "Test",
            LaboratorioId: Guid.NewGuid(),
            UnidadBaseId: Guid.NewGuid(),
            CondicionVenta: CondicionVenta.VentaLibreOTC,
            PrecioVentaBase: 15m);

        // Act
        var result = await Sender.Send(command);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task CreateMedicamento_ShouldRollback_WhenExceptionOccursDuringTransaction()
    {
        // Este test verifica que el TransactionBehavior hace rollback correctamente.
        // Enviamos un command con LaboratorioId inválido que causará FK violation en SaveChanges.
        var unidadId = await SeedUnidadMedidaAsync();

        var command = new CreateMedicamentoCommand(
            CodigoNacional: $"TX-{Guid.NewGuid():N}"[..20],
            NombreComercial: "Test Rollback",
            LaboratorioId: Guid.NewGuid(), // FK inválido — no existe en BD
            UnidadBaseId: unidadId,
            CondicionVenta: CondicionVenta.VentaLibreOTC,
            PrecioVentaBase: 15m);

        // Act
        var act = async () => await Sender.Send(command);

        // Assert — debe lanzar excepción por FK violation (no un Result failure)
        _ = await act.Should().ThrowAsync<Exception>(because: "FK violation debe hacer rollback y propagar la excepción");

        // Verificar que no quedó nada en la BD
        var count = await DbContext.Medicamentos
            .CountAsync(m => m.NombreComercial == "Test Rollback");
        _ = count.Should().Be(0, because: "el rollback debe haber deshecho la inserción");
    }

    // Helpers de seeding para el entorno de test
    private async Task<Guid> SeedLaboratorioAsync()
    {
        var laboratorio = Domain.Entities.Laboratorio.Create(
            $"LabTest{Guid.NewGuid():N}"[..20],
            "LAB-TEST").Value!;

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
}
