using SOFIA.Application.Empleados.Commands.CreateEmpleado;
using SOFIA.Application.Empleados.Queries.GetById;
using SOFIA.Application.Empleados.Queries.GetEmpleadosWithPagination;
using SOFIA.Application.Security.Queries.Cuentas.GetCuentaById;
using SOFIA.Application.Security.Queries.Cuentas.GetCuentas;
using SOFIA.Application.Sucursales.Queries.GetById;
using SOFIA.Application.Sucursales.Queries.GetSucursalesWithPagination;
using SOFIA.Domain.Entities;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Empleados;

public class EmpleadoSinApellidoMaternoIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task CreateEmpleado_ShouldStoreNullAndReadBackACleanName_WhenApellidoMaternoIsBlank()
    {
        // Arrange: a fresh company keeps employees seeded by other tests out of the list
        var sucursal = await SeedSucursalAsync();

        // Act
        var sinMaterno = await Sender.Send(NewCommand(sucursal.Id, "Ana", "   "));
        var conMaterno = await Sender.Send(NewCommand(sucursal.Id, "Luis", " Rojas "));
        var lista = await Sender.Send(new GetEmpleadosWithPaginationQuery());
        var detalle = await Sender.Send(new GetEmpleadoByIdQuery(sinMaterno.Value));

        // Assert
        _ = sinMaterno.IsSuccess.Should().BeTrue();
        _ = conMaterno.IsSuccess.Should().BeTrue();
        _ = lista.Value!.Items.Select(e => e.Nombre_Completo).Should().BeEquivalentTo("Ana Quispe", "Luis Quispe Rojas");
        _ = lista.Value.Items.Single(e => e.Id == sinMaterno.Value).Apellido_Materno.Should().BeNull();
        _ = detalle.Value!.Apellido_Materno.Should().BeNull();
        _ = detalle.Value.Nombre_Completo.Should().Be("Ana Quispe");
    }

    [Fact]
    public async Task GetEmpleados_ShouldFilterByApellidoMaternoWithoutFailing_WhenSomeEmpleadosHaveNone()
    {
        // Arrange
        var sucursal = await SeedSucursalAsync();
        _ = await Sender.Send(NewCommand(sucursal.Id, "Ana", null));
        var conMaterno = await Sender.Send(NewCommand(sucursal.Id, "Luis", "Rojas"));

        // Act
        var porApellidoMaterno = await Sender.Send(new GetEmpleadosWithPaginationQuery { ApellidoMaterno = "Roj" });
        var porTermino = await Sender.Send(new GetEmpleadosWithPaginationQuery { SearchTerm = "Roj" });

        // Assert
        _ = porApellidoMaterno.Value!.Items.Select(e => e.Id).Should().Equal(conMaterno.Value);
        _ = porTermino.Value!.Items.Select(e => e.Id).Should().Equal(conMaterno.Value);
    }

    [Fact]
    public async Task CuentasAndSucursales_ShouldShowTheNameWithoutAGap_WhenTheEmpleadoHasNoApellidoMaterno()
    {
        // Arrange: the employee without second surname owns an account and manages the branch
        var sucursal = await SeedSucursalAsync();
        var empleadoId = (await Sender.Send(NewCommand(sucursal.Id, "Ana", null))).Value;
        var cuenta = Cuenta.Create(empleadoId, $"u{Guid.NewGuid():N}"[..20], "hash").Value!;
        _ = DbContext.Cuentas.Add(cuenta);
        _ = sucursal.Update(sucursal.Nombre, sucursal.Direccion_Fisica, sucursal.Numero_Licencia, empleadoId);
        _ = await DbContext.SaveChangesAsync();

        // Act
        var cuentas = await Sender.Send(new GetCuentasQuery { NombreEmpleado = "Ana Quispe" });
        var cuentaDetalle = await Sender.Send(new GetCuentaByIdQuery(cuenta.Id));
        var sucursales = await Sender.Send(new GetSucursalesWithPaginationQuery());
        var sucursalDetalle = await Sender.Send(new GetSucursalByIdQuery(sucursal.Id));

        // Assert
        _ = cuentas.Value!.Items.Should().ContainSingle().Which.NombreEmpleado.Should().Be("Ana Quispe");
        _ = cuentaDetalle.Value!.NombreEmpleado.Should().Be("Ana Quispe");
        _ = sucursales.Value!.Items.Should().ContainSingle().Which.GerenteNombre.Should().Be("Ana Quispe");
        _ = sucursalDetalle.Value!.GerenteNombre.Should().Be("Ana Quispe");
    }

    private async Task<Sucursal> SeedSucursalAsync()
    {
        CurrentUser.Empresa = Guid.NewGuid();
        var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = DbContext.Sucursales.Add(sucursal);
        _ = await DbContext.SaveChangesAsync();
        return sucursal;
    }

    private static CreateEmpleadoCommand NewCommand(Guid sucursalId, string nombres, string? apellidoMaterno) => new()
    {
        Sucursal_Base_ID = sucursalId,
        Nombres = nombres,
        Apellido_Paterno = "Quispe",
        Apellido_Materno = apellidoMaterno,
    };
}
