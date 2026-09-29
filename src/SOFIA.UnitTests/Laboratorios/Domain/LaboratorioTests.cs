using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Laboratorios.Domain;

public class LaboratorioTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldStoreNullCodigo_WhenCodigoIsBlank(string codigo)
    {
        var laboratorio = Laboratorio.Create("Bayer", codigo).Value!;

        _ = laboratorio.CodigoIdentificador.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldStoreNullCodigo_WhenCodigoIsBlank()
    {
        var laboratorio = Laboratorio.Create("Bayer", "BAY-01").Value!;

        _ = laboratorio.Update("Bayer", " ");

        _ = laboratorio.CodigoIdentificador.Should().BeNull();
    }
}
