using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.FormulacionesClinicas.Domain;

public class FormulacionClinicaTests
{
    private static readonly Guid ProductoId = Guid.NewGuid();
    private static readonly Guid IngredienteId = Guid.NewGuid();
    private static readonly Guid UnidadMedidaId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSucceed_WhenAllFieldsValid()
    {
        var result = FormulacionClinica.Create(ProductoId, IngredienteId, 100m, UnidadMedidaId, "TE-045");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.ProductoId.Should().Be(ProductoId);
        _ = result.Value.IngredienteId.Should().Be(IngredienteId);
        _ = result.Value.ConcentracionDosis.Should().Be(100m);
        _ = result.Value.UnidadMedidaId.Should().Be(UnidadMedidaId);
        _ = result.Value.CodigoTeOrange.Should().Be("TE-045");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenCodigoTeOrangeIsNull()
    {
        var result = FormulacionClinica.Create(ProductoId, IngredienteId, 100m, UnidadMedidaId, null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.CodigoTeOrange.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldFail_WhenProductoIdEmpty()
    {
        var result = FormulacionClinica.Create(Guid.Empty, IngredienteId, 100m, UnidadMedidaId, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.ProductoId");
    }

    [Fact]
    public void Create_ShouldFail_WhenIngredienteIdEmpty()
    {
        var result = FormulacionClinica.Create(ProductoId, Guid.Empty, 100m, UnidadMedidaId, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.IngredienteId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_ShouldFail_WhenConcentracionDosisNotPositive(decimal concentracion)
    {
        var result = FormulacionClinica.Create(ProductoId, IngredienteId, concentracion, UnidadMedidaId, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.Concentracion");
    }

    [Fact]
    public void Create_ShouldFail_WhenUnidadMedidaIdEmpty()
    {
        var result = FormulacionClinica.Create(ProductoId, IngredienteId, 100m, Guid.Empty, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.UnidadMedidaId");
    }

    [Fact]
    public void Update_ShouldMutateFields_WhenValid()
    {
        var formulacion = FormulacionClinica.Create(ProductoId, IngredienteId, 100m, UnidadMedidaId, "TE-045").Value!;
        var newIngrediente = Guid.NewGuid();
        var newUnidad = Guid.NewGuid();

        var result = formulacion.Update(newIngrediente, 250m, newUnidad, "TE-050");

        _ = result.IsSuccess.Should().BeTrue();
        _ = formulacion.IngredienteId.Should().Be(newIngrediente);
        _ = formulacion.ConcentracionDosis.Should().Be(250m);
        _ = formulacion.UnidadMedidaId.Should().Be(newUnidad);
        _ = formulacion.CodigoTeOrange.Should().Be("TE-050");
    }

    [Fact]
    public void Update_ShouldFail_AndNotMutate_WhenIngredienteIdEmpty()
    {
        var formulacion = FormulacionClinica.Create(ProductoId, IngredienteId, 100m, UnidadMedidaId, null).Value!;

        var result = formulacion.Update(Guid.Empty, 250m, UnidadMedidaId, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.IngredienteId");
        _ = formulacion.IngredienteId.Should().Be(IngredienteId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Update_ShouldFail_WhenConcentracionDosisNotPositive(decimal concentracion)
    {
        var formulacion = FormulacionClinica.Create(ProductoId, IngredienteId, 100m, UnidadMedidaId, null).Value!;

        var result = formulacion.Update(IngredienteId, concentracion, UnidadMedidaId, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.Concentracion");
        _ = formulacion.ConcentracionDosis.Should().Be(100m);
    }

    [Fact]
    public void Update_ShouldFail_WhenUnidadMedidaIdEmpty()
    {
        var formulacion = FormulacionClinica.Create(ProductoId, IngredienteId, 100m, UnidadMedidaId, null).Value!;

        var result = formulacion.Update(IngredienteId, 250m, Guid.Empty, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.UnidadMedidaId");
        _ = formulacion.UnidadMedidaId.Should().Be(UnidadMedidaId);
    }
}
