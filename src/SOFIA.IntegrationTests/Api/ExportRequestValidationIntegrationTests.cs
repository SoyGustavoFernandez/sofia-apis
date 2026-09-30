using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using SOFIA.API.Infrastructure;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Api;

public class ExportRequestValidationIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Theory]
    [InlineData("dd/MM/yyyy")]
    [InlineData("MM/dd/yyyy HH:mm")]
    public async Task Exportar_ShouldReturnWorkbook_WhenDateFormatIsAllowed(string dateFormat)
    {
        var client = await CreateClientForNewCompanyAsync();

        var response = await client.PostAsJsonAsync("/api/v1/pacientes/exportar", new { headers = new[] { "Codigo" }, dateFormat });

        _ = response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("%")]
    [InlineData("yyyy-MM-dd")]
    public async Task Exportar_ShouldReturnBadRequest_WhenDateFormatIsNotAllowed(string dateFormat)
    {
        var client = await CreateClientForNewCompanyAsync();

        var response = await client.PostAsJsonAsync("/api/v1/pacientes/exportar", new { headers = new[] { "Codigo" }, dateFormat });

        _ = response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = (await response.Content.ReadAsStringAsync()).Should().Contain("Export.DateFormat.Invalid");
    }

    [Fact]
    public async Task Exportar_ShouldReturnBadRequest_WhenHeadersExceedLimits()
    {
        var client = await CreateClientForNewCompanyAsync();
        var tooMany = Enumerable.Repeat("Col", ExportHeadersAttribute.MaxHeaders + 1).ToArray();
        var tooLong = new[] { new string('x', ExportHeadersAttribute.MaxHeaderLength + 1) };

        var manyResponse = await client.PostAsJsonAsync("/api/v1/laboratorios/exportar", new { headers = tooMany });
        var longResponse = await client.PostAsJsonAsync("/api/v1/laboratorios/exportar", new { headers = tooLong });

        _ = manyResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = longResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _ = (await manyResponse.Content.ReadAsStringAsync()).Should().Contain("Export.Headers.Invalid");
    }

    [Theory]
    [InlineData("dd/MM/yyyy", true)]
    [InlineData("MM/dd/yyyy", true)]
    [InlineData("dd/MM/yyyy HH:mm", true)]
    [InlineData("MM/dd/yyyy HH:mm", true)]
    [InlineData("DD/MM/YYYY", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ExportDateFormatAttribute_ShouldAcceptOnlyKnownFormats(string? format, bool expected) =>
        new ExportDateFormatAttribute().IsValid(format).Should().Be(expected);

    // Each test gets its own company so its export never sees another test's rows
    private async Task<HttpClient> CreateClientForNewCompanyAsync()
    {
        CurrentUser.Empresa = null;
        var usuario = $"expval_{Guid.NewGuid():N}"[..20];
        var registro = await Sender.Send(new RegistrarEmpresaCommand
        {
            NombreEmpresa = $"Farmacia {usuario}",
            Usuario = usuario,
            Password = "TestPassword123!",
        });
        _ = registro.IsSuccess.Should().BeTrue();

        var client = Factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registro.Value!.AccessToken);
        return client;
    }
}
