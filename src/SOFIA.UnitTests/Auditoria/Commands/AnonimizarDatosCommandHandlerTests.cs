using FluentAssertions;
using SOFIA.Application.Auditoria.Commands.AnonimizarDatos;

namespace SOFIA.UnitTests.Auditoria.Commands;

public class AnonimizarDatosCommandHandlerTests
{
    [Fact]
    public async Task Handle_Always_ReturnsNotImplementedInsteadOfFakeSuccess()
    {
        var handler = new AnonimizarDatosCommandHandler();

        var result = await handler.Handle(new AnonimizarDatosCommand("Pacientes_Clientes", Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(501);
        _ = result.Error.Code.Should().Be("Auditoria.Anonimizacion.NoImplementada");
    }
}
