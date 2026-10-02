using SOFIA.Application.Pacientes.Queries.GetPacientes;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Pacientes;

public class GetPacientesIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task Handle_ShouldPageAlphabeticallyWithoutRepeatingOrSkipping_WhenSeveralPatientsShareAName()
    {
        // A fresh company keeps patients seeded by other tests out of the pages
        CurrentUser.Empresa = Guid.NewGuid();
        await SeedAsync("Carla Rojas", "Ana Diaz", "Bruno Paz", "Ana Diaz", "Ana Diaz");

        var paginas = new List<PacienteDto>();
        for (var pagina = 1; pagina <= 3; pagina++)
        {
            var result = await Sender.Send(new GetPacientesQuery { PageNumber = pagina, PageSize = 2 });
            _ = result.IsSuccess.Should().BeTrue();
            paginas.AddRange(result.Value!.Items);
        }

        var completo = await Sender.Send(new GetPacientesQuery { PageSize = 5 });

        _ = paginas.Select(x => x.NombreApellidos).Should().Equal("Ana Diaz", "Ana Diaz", "Ana Diaz", "Bruno Paz", "Carla Rojas");
        _ = paginas.Select(x => x.Id).Should().OnlyHaveUniqueItems();
        _ = paginas.Select(x => x.Id).Should().Equal(completo.Value!.Items.Select(x => x.Id));
    }

    private async Task SeedAsync(params string[] nombres)
    {
        var primerDni = Random.Shared.Next(10_000_000, 99_999_000);
        DbContext.Pacientes.AddRange(nombres.Select((nombre, i) =>
            PacienteCliente.Create(TipoDocumentoIdentidad.Dni, $"{primerDni + i}", nombre, new DateOnly(1990, 1, 1), null).Value!));
        _ = await DbContext.SaveChangesAsync();
    }
}
