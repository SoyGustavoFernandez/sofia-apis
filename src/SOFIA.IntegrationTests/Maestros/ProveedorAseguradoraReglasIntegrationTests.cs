using SOFIA.Application.Proveedores.Commands.CreateProveedor;
using SOFIA.Application.Seguros.Commands.CargaMasivaSeguros;
using SOFIA.Application.Seguros.Commands.CreateAseguradora;
using SOFIA.Application.Seguros.Commands.UpdateAseguradora;
using SOFIA.Application.Seguros.Common;
using SOFIA.Domain.Entities;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Maestros;

public class ProveedorAseguradoraReglasIntegrationTests : BaseIntegrationTest
{
    // Every test runs in its own company
    public ProveedorAseguradoraReglasIntegrationTests(SofiaWebAppFactory factory) : base(factory) => CurrentUser.Empresa = Guid.NewGuid();

    [Theory]
    [InlineData("123")]
    [InlineData("2010000000A")]
    [InlineData("201000000012")]
    public async Task CreateProveedor_ShouldReject_WhenTheTaxIdIsNotAnElevenDigitRuc(string taxId)
    {
        var result = await Sender.Send(new CreateProveedorCommand("Distribuidora Norte", taxId, null, null));

        _ = result.Error.Code.Should().Be("CreateProveedorCommand.TaxId");
        _ = result.StatusCode.Should().Be(400);
        _ = ProveedorDistribuidor.Create("Distribuidora Norte", taxId, null, null).IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task CreateProveedor_ShouldReject_WhenFinancialTermsExceedTheColumn()
    {
        var result = await Sender.Send(new CreateProveedorCommand("Distribuidora Sur", "20100000001", new string('a', 101), null));

        _ = result.Error.Code.Should().Be("CreateProveedorCommand.TerminosFinancieros");
    }

    [Fact]
    public async Task Aseguradora_ShouldRepeatItsCodeOnlyAcrossCompanies()
    {
        var primera = await Sender.Send(new CreateAseguradoraCommand("Salud Total", "COD-1"));
        var repetida = await Sender.Send(new CreateAseguradoraCommand("Otro Nombre", "COD-1"));
        CurrentUser.Empresa = Guid.NewGuid();
        var otraEmpresa = await Sender.Send(new CreateAseguradoraCommand("Salud Total", "COD-1"));

        _ = primera.IsSuccess.Should().BeTrue();
        _ = repetida.Error.Code.Should().Be(AseguradoraDuplicateChecker.CodigoDuplicado);
        _ = repetida.StatusCode.Should().Be(409);
        _ = otraEmpresa.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateAseguradora_ShouldKeepItsOwnCodeButNotTakeAnothers()
    {
        var primera = await Sender.Send(new CreateAseguradoraCommand("Uno", "COD-A"));
        _ = await Sender.Send(new CreateAseguradoraCommand("Dos", "COD-B"));

        var mismoCodigo = await Sender.Send(new UpdateAseguradoraCommand(primera.Value, "Uno renombrada", "COD-A"));
        var codigoAjeno = await Sender.Send(new UpdateAseguradoraCommand(primera.Value, "Uno", "COD-B"));

        _ = mismoCodigo.IsSuccess.Should().BeTrue();
        _ = codigoAjeno.Error.Code.Should().Be(AseguradoraDuplicateChecker.CodigoDuplicado);
    }

    [Fact]
    public async Task CargaMasivaSeguros_ShouldSkipCodesAlreadyUsedOrRepeatedInTheFile()
    {
        _ = await Sender.Send(new CreateAseguradoraCommand("Existente", "COD-X"));

        var result = await Sender.Send(new CargaMasivaSegurosCommand(
        [
            new("Repetida", "COD-X"),
            new("Nueva", "COD-Y"),
            new("Nueva otra vez", "COD-Y"),
        ]));

        _ = result.Value.Should().Be(1);
    }
}
