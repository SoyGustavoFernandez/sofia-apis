using System.Net;
using System.Net.Http.Headers;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Api;

public class NotFoundResponseIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task GetRecetaById_ShouldReturnNotFound_WhenTheRecordDoesNotExist()
    {
        var client = await CreateClientForNewCompanyAsync();

        var response = await client.GetAsync($"/api/v1/recetas/{Guid.NewGuid()}");

        _ = response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _ = (await response.Content.ReadAsStringAsync()).Should().Contain("RecetaMedica.NotFound");
    }

    private async Task<HttpClient> CreateClientForNewCompanyAsync()
    {
        CurrentUser.Empresa = null;
        var usuario = $"notfound_{Guid.NewGuid():N}"[..20];
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
