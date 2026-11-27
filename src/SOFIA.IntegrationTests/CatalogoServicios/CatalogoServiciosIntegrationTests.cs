using SOFIA.Application.CatalogoServicios.Commands.CreateServicioCatalogo;
using SOFIA.Application.CatalogoServicios.Commands.DeleteServicioCatalogo;
using SOFIA.Application.CatalogoServicios.Commands.UpdateServicioCatalogo;
using SOFIA.Application.CatalogoServicios.Common;
using SOFIA.Application.CatalogoServicios.Queries.GetServiciosCatalogo;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.CatalogoServicios;

public class CatalogoServiciosIntegrationTests : BaseIntegrationTest
{
    // Every test runs in its own company
    public CatalogoServiciosIntegrationTests(SofiaWebAppFactory factory) : base(factory) => CurrentUser.Empresa = Guid.NewGuid();

    private static CreateServicioCatalogoCommand Inyeccion(string nombre = "Aplicación de inyección") => new(nombre, 5m);

    [Fact]
    public async Task Names_ShouldBeUniqueOnlyWithinACompany()
    {
        var primero = await Sender.Send(Inyeccion());
        var repetido = await Sender.Send(Inyeccion("  Aplicación de inyección "));
        CurrentUser.Empresa = Guid.NewGuid();
        var otraEmpresa = await Sender.Send(Inyeccion());

        _ = primero.IsSuccess.Should().BeTrue();
        _ = repetido.Error.Code.Should().Be(ServicioCatalogoDuplicateChecker.NombreDuplicado);
        _ = otraEmpresa.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Create_ShouldReject_WhenThePriceIsNotPositive(int precio)
    {
        var result = await Sender.Send(Inyeccion() with { Precio = precio });

        _ = result.Error.Code.Should().Be("CreateServicioCatalogoCommand.Precio");
    }

    [Fact]
    public async Task Search_ShouldFilterAndForgetDeletedServices()
    {
        var inyeccion = await Sender.Send(Inyeccion());
        var presion = await Sender.Send(new CreateServicioCatalogoCommand("Toma de presión", 3m));
        _ = await Sender.Send(new UpdateServicioCatalogoCommand(presion.Value, "Toma de presión arterial", 4m));

        var baratos = await Sender.Send(new GetServiciosCatalogoQuery { PrecioMax = 4m });
        _ = await Sender.Send(new DeleteServicioCatalogoCommand(inyeccion.Value));
        var todos = await Sender.Send(new GetServiciosCatalogoQuery());

        _ = baratos.Value!.Items.Should().ContainSingle().Which.Nombre.Should().Be("Toma de presión arterial");
        _ = todos.Value!.Items.Should().ContainSingle();
    }
}
