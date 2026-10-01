using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed class PacienteCliente : BaseEntity
{
    private PacienteCliente() { } // Required for EF Core

    // SUNAT catálogo 06 code; null only on patients registered before the document type was captured
    public string? TipoDocumento { get; private set; }
    public string DocIdentidadGub { get; private set; } = string.Empty;
    public string NombreApellidos { get; private set; } = string.Empty;
    public DateOnly FechaNacimiento { get; private set; }
    public string? ContactoPrimario { get; private set; }

    public static Result<PacienteCliente> Create(
        string? tipoDocumento,
        string docIdentidadGub,
        string nombreApellidos,
        DateOnly fechaNacimiento,
        string? contactoPrimario)
    {
        var error = Validate(tipoDocumento, docIdentidadGub, nombreApellidos, fechaNacimiento, contactoPrimario);
        if (error is not null)
        {
            return Result.Failure<PacienteCliente>(error);
        }

        return Result.Success(new PacienteCliente
        {
            TipoDocumento = tipoDocumento,
            DocIdentidadGub = docIdentidadGub,
            NombreApellidos = nombreApellidos,
            FechaNacimiento = fechaNacimiento,
            ContactoPrimario = contactoPrimario
        });
    }

    public Result Update(
        string? tipoDocumento,
        string docIdentidadGub,
        string nombreApellidos,
        DateOnly fechaNacimiento,
        string? contactoPrimario)
    {
        var error = Validate(tipoDocumento, docIdentidadGub, nombreApellidos, fechaNacimiento, contactoPrimario);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        TipoDocumento = tipoDocumento;
        DocIdentidadGub = docIdentidadGub;
        NombreApellidos = nombreApellidos;
        FechaNacimiento = fechaNacimiento;
        ContactoPrimario = contactoPrimario;

        return Result.Success();
    }

    private static Error? Validate(
        string? tipoDocumento,
        string docIdentidadGub,
        string nombreApellidos,
        DateOnly fechaNacimiento,
        string? contactoPrimario)
    {
        if (string.IsNullOrWhiteSpace(docIdentidadGub))
        {
            return Error.Validation("PacienteCliente.DocIdentidadGub", "Documento de Identidad Gubernamental is required.");
        }

        if (docIdentidadGub.Length > 50)
        {
            return Error.Validation("PacienteCliente.DocIdentidadGub", "Documento de Identidad must not exceed 50 characters.");
        }

        if (!TipoDocumentoIdentidad.EsValido(tipoDocumento))
        {
            return Error.Validation("PacienteCliente.TipoDocumento", "Tipo de Documento must be DNI (1), CE (4), RUC (6) or Pasaporte (7).");
        }

        if (!TipoDocumentoIdentidad.EsNumeroValido(tipoDocumento, docIdentidadGub))
        {
            return Error.Validation("PacienteCliente.DocIdentidadGub.FormatoInvalido", "Documento de Identidad does not match the format of its type.");
        }

        if (string.IsNullOrWhiteSpace(nombreApellidos))
        {
            return Error.Validation("PacienteCliente.NombreApellidos", "Nombre y Apellidos is required.");
        }

        if (nombreApellidos.Length > 200)
        {
            return Error.Validation("PacienteCliente.NombreApellidos", "Nombre y Apellidos must not exceed 200 characters.");
        }

        if (fechaNacimiento == default)
        {
            return Error.Validation("PacienteCliente.FechaNacimiento", "Fecha de Nacimiento is required.");
        }

        if (fechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Error.Validation("PacienteCliente.FechaNacimiento", "Fecha de Nacimiento cannot be in the future.");
        }

        if (contactoPrimario != null && contactoPrimario.Length > 100)
        {
            return Error.Validation("PacienteCliente.ContactoPrimario", "Contacto Primario must not exceed 100 characters.");
        }

        return null;
    }
}
