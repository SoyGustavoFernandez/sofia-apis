using FluentAssertions;
using SOFIA.Application.Pacientes.Commands.CreatePaciente;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Pacientes.Commands.CreatePaciente;

public class CreatePacienteCommandValidatorTests
{
    private readonly CreatePacienteCommandValidator _validator = new();

    private static CreatePacienteCommand Command(string? tipo, string numero) => new(tipo, numero, "Juan Perez", new DateOnly(1990, 1, 1), null);

    [Theory]
    [InlineData(TipoDocumentoIdentidad.Dni, "45781236")]
    [InlineData(TipoDocumentoIdentidad.Ruc, "20212971893")]
    [InlineData(TipoDocumentoIdentidad.CarneExtranjeria, "CE0012345678")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB1234567")]
    public void Validate_ShouldPass_WhenNumberMatchesItsDocumentType(string tipo, string numero) =>
        _ = _validator.Validate(Command(tipo, numero)).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("9")]
    public void Validate_ShouldFail_WhenDocumentTypeIsMissingOrUnknown(string? tipo)
    {
        var result = _validator.Validate(Command(tipo, "45781236"));

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePacienteCommand.TipoDocumento));
    }

    [Theory]
    [InlineData(TipoDocumentoIdentidad.Dni, "1234567")]
    [InlineData(TipoDocumentoIdentidad.Ruc, "123456789012")]
    [InlineData(TipoDocumentoIdentidad.CarneExtranjeria, "CE00123456789")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB-123")]
    public void Validate_ShouldFail_WhenNumberDoesNotMatchItsDocumentType(string tipo, string numero)
    {
        var result = _validator.Validate(Command(tipo, numero));

        _ = result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(CreatePacienteCommand.DocIdentidadGub));
    }
}
