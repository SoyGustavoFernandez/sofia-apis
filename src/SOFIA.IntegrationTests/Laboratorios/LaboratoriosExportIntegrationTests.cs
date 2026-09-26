using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.Domain.Entities;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Laboratorios;

public class LaboratoriosExportIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Exportar_ShouldRejectWithTooManyRows_WhenResultsExceedExportLimit()
    {
        var client = await CreateClientForNewCompanyAsync(PaginationLimits.MaxPageSize + 1);

        var response = await client.PostAsJsonAsync("/api/v1/laboratorios/exportar", new { headers = new[] { "Nombre", "Codigo" } });

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        _ = body.RootElement.GetProperty("code").GetString().Should().Be("Export.TooManyRows");
    }

    [Fact]
    public async Task Exportar_ShouldReturnWorkbook_WhenResultsFitExportLimit()
    {
        var client = await CreateClientForNewCompanyAsync(3);

        var response = await client.PostAsJsonAsync("/api/v1/laboratorios/exportar", new { headers = new[] { "Nombre", "Codigo" } });

        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
        _ = response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    // Each test gets its own company so the seeded rows never leak into another test's export
    private async Task<HttpClient> CreateClientForNewCompanyAsync(int laboratorios)
    {
        CurrentUser.Empresa = null;
        var usuario = $"export_{Guid.NewGuid():N}"[..20];
        var registro = await Sender.Send(new RegistrarEmpresaCommand
        {
            NombreEmpresa = $"Farmacia {usuario}",
            Usuario = usuario,
            Password = "TestPassword123!",
        });
        _ = registro.IsSuccess.Should().BeTrue();

        CurrentUser.Empresa = DbContext.Cuentas.Local.Single(c => c.NombreUsuario == usuario).TenantId;
        DbContext.Laboratorios.AddRange(Enumerable.Range(1, laboratorios)
            .Select(i => Laboratorio.Create($"Lab {usuario} {i}", null).Value!));
        _ = await DbContext.SaveChangesAsync();

        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registro.Value!.AccessToken);
        return client;
    }
}
