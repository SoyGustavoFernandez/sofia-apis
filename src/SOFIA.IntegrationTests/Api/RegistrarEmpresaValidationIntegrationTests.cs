using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SOFIA.Domain.ValueObjects;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Api;

public class RegistrarEmpresaValidationIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Registrar_ShouldReturnBadRequestWithAddressCode_WhenDireccionSedeIsMissing()
    {
        var usuario = $"sinsede_{Guid.NewGuid():N}"[..20];
        var body = new
        {
            nombreEmpresa = $"Farmacia {usuario}",
            ruc = RandomRuc(),
            usuario,
            password = RegistroEmpresaBuilder.Password,
            adminNombres = "Admin",
            adminApellidoPaterno = "Prueba",
            adminEmail = $"{usuario}@sofia.test"
        };

        var response = await CreateClient().PostAsJsonAsync("/api/v1/empresas/registrar", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _ = json.RootElement.GetProperty("code").GetString().Should().Be("RegistrarEmpresaCommand.DireccionSede");
    }

    [Fact]
    public async Task Registrar_ShouldStoreTheSentAddressOnTheMainBranch_WhenDireccionSedeIsProvided()
    {
        var usuario = $"consede_{Guid.NewGuid():N}"[..20];
        var ruc = RandomRuc();
        var body = new
        {
            nombreEmpresa = $"Farmacia {usuario}",
            ruc,
            usuario,
            password = RegistroEmpresaBuilder.Password,
            adminNombres = "Admin",
            adminApellidoPaterno = "Prueba",
            adminEmail = $"{usuario}@sofia.test",
            direccionSede = "  Av. Los Olivos 123, Lima  "
        };

        var response = await CreateClient().PostAsJsonAsync("/api/v1/empresas/registrar", body);

        _ = response.StatusCode.Should().Be(HttpStatusCode.Created);
        var rucVo = Ruc.Create(ruc).Value;
        var empresa = await DbContext.Empresas.IgnoreQueryFilters().SingleAsync(e => e.RUC == rucVo);
        var sede = await DbContext.Sucursales.IgnoreQueryFilters().SingleAsync(s => s.EmpresaId == empresa.Id);
        _ = sede.Direccion_Fisica.Should().Be("Av. Los Olivos 123, Lima");
    }

    private HttpClient CreateClient() => Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

    private static string RandomRuc() => string.Concat(Enumerable.Range(0, 11).Select(_ => Random.Shared.Next(10)));
}
