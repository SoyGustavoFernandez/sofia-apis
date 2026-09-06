using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Domain;

public class MedicamentoTests
{
    private static readonly Guid LabId = Guid.NewGuid();
    private static readonly Guid UnidadId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSucceed_WhenAllFieldsValid()
    {
        var result = Medicamento.Create("COD-001", "Paracetamol 500mg", LabId, UnidadId, CondicionVenta.RecetaSimple);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.CodigoNacional.Should().Be("COD-001");
        _ = result.Value.NombreComercial.Should().Be("Paracetamol 500mg");
        _ = result.Value.LaboratorioId.Should().Be(LabId);
        _ = result.Value.UnidadBaseId.Should().Be(UnidadId);
        _ = result.Value.CondicionVenta.Should().Be(CondicionVenta.RecetaSimple);
    }

    [Theory]
    [InlineData("", "Medicamento.CodigoNacional")]
    [InlineData("   ", "Medicamento.CodigoNacional")]
    public void Create_ShouldFail_WhenCodigoNacionalMissing(string codigo, string expectedCode)
    {
        var result = Medicamento.Create(codigo, "Nombre", LabId, UnidadId, CondicionVenta.VentaLibreOTC);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be(expectedCode);
    }

    [Fact]
    public void Create_ShouldFail_WhenCodigoNacionalTooLong()
    {
        var result = Medicamento.Create(new string('x', Medicamento.CodigoNacionalMaxLength + 1), "Nombre", LabId, UnidadId, CondicionVenta.VentaLibreOTC);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.CodigoNacional");
    }

    [Fact]
    public void Create_ShouldFail_WhenNombreComercialMissing()
    {
        var result = Medicamento.Create("COD-001", "  ", LabId, UnidadId, CondicionVenta.VentaLibreOTC);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.NombreComercial");
    }

    [Fact]
    public void Create_ShouldFail_WhenNombreComercialTooLong()
    {
        var result = Medicamento.Create("COD-001", new string('x', Medicamento.NombreComercialMaxLength + 1), LabId, UnidadId, CondicionVenta.VentaLibreOTC);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.NombreComercial");
    }

    [Fact]
    public void Create_ShouldFail_WhenLaboratorioIdEmpty()
    {
        var result = Medicamento.Create("COD-001", "Nombre", Guid.Empty, UnidadId, CondicionVenta.VentaLibreOTC);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.LaboratorioId");
    }

    [Fact]
    public void Create_ShouldFail_WhenUnidadBaseIdEmpty()
    {
        var result = Medicamento.Create("COD-001", "Nombre", LabId, Guid.Empty, CondicionVenta.VentaLibreOTC);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.UnidadBaseId");
    }

    [Fact]
    public void Update_ShouldMutateFields_WhenValid()
    {
        var medicamento = Medicamento.Create("COD-001", "Old", LabId, UnidadId, CondicionVenta.VentaLibreOTC).Value!;
        var newLab = Guid.NewGuid();
        var newUnidad = Guid.NewGuid();

        var result = medicamento.Update("COD-002", "New", newLab, newUnidad, CondicionVenta.RecetaRetenida);

        _ = result.IsSuccess.Should().BeTrue();
        _ = medicamento.CodigoNacional.Should().Be("COD-002");
        _ = medicamento.NombreComercial.Should().Be("New");
        _ = medicamento.LaboratorioId.Should().Be(newLab);
        _ = medicamento.UnidadBaseId.Should().Be(newUnidad);
        _ = medicamento.CondicionVenta.Should().Be(CondicionVenta.RecetaRetenida);
    }

    [Fact]
    public void Update_ShouldFail_AndNotMutate_WhenInvalid()
    {
        var medicamento = Medicamento.Create("COD-001", "Original", LabId, UnidadId, CondicionVenta.VentaLibreOTC).Value!;

        var result = medicamento.Update("", "Original", LabId, UnidadId, CondicionVenta.VentaLibreOTC);

        _ = result.IsFailure.Should().BeTrue();
        _ = medicamento.CodigoNacional.Should().Be("COD-001");
    }

    [Fact]
    public void CondicionesValidas_ShouldMatchEnumDescriptions() => _ = Medicamento.CondicionesValidas.Should().ContainInOrder(
            "Venta Libre (OTC)", "Receta Simple", "Receta Retenida", "Estupefaciente");
}
