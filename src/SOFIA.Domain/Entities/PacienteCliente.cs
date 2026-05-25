using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class PacienteCliente : BaseEntity
{
    private PacienteCliente() { } // Required for EF Core

    public string DocIdentidadGub { get; private set; } = string.Empty;
    public string NombreApellidos { get; private set; } = string.Empty;
    public DateOnly FechaNacimiento { get; private set; }
    public string? ContactoPrimario { get; private set; }

    public static Result<PacienteCliente> Create(
        string docIdentidadGub,
        string nombreApellidos,
        DateOnly fechaNacimiento,
        string? contactoPrimario) =>
        string.IsNullOrWhiteSpace(docIdentidadGub)
            ? Result.Failure<PacienteCliente>(Error.Validation("PacienteCliente.DocIdentidadGub", "Documento de Identidad Gubernamental is required."))
            : docIdentidadGub.Length > 50
            ? Result.Failure<PacienteCliente>(Error.Validation("PacienteCliente.DocIdentidadGub", "Documento de Identidad must not exceed 50 characters."))
            : string.IsNullOrWhiteSpace(nombreApellidos)
            ? Result.Failure<PacienteCliente>(Error.Validation("PacienteCliente.NombreApellidos", "Nombre y Apellidos is required."))
            : nombreApellidos.Length > 200
            ? Result.Failure<PacienteCliente>(Error.Validation("PacienteCliente.NombreApellidos", "Nombre y Apellidos must not exceed 200 characters."))
            : fechaNacimiento == default
            ? Result.Failure<PacienteCliente>(Error.Validation("PacienteCliente.FechaNacimiento", "Fecha de Nacimiento is required."))
            : fechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow)
            ? Result.Failure<PacienteCliente>(Error.Validation("PacienteCliente.FechaNacimiento", "Fecha de Nacimiento cannot be in the future."))
            : contactoPrimario != null && contactoPrimario.Length > 100
            ? Result.Failure<PacienteCliente>(Error.Validation("PacienteCliente.ContactoPrimario", "Contacto Primario must not exceed 100 characters."))
            : Result.Success(new PacienteCliente
            {
                DocIdentidadGub = docIdentidadGub,
                NombreApellidos = nombreApellidos,
                FechaNacimiento = fechaNacimiento,
                ContactoPrimario = contactoPrimario
            });

    public Result Update(
        string docIdentidadGub,
        string nombreApellidos,
        DateOnly fechaNacimiento,
        string? contactoPrimario)
    {
        if (string.IsNullOrWhiteSpace(docIdentidadGub))
        {
            return Result.Failure(Error.Validation("PacienteCliente.DocIdentidadGub", "Documento de Identidad Gubernamental is required."));
        }

        if (docIdentidadGub.Length > 50)
        {
            return Result.Failure(Error.Validation("PacienteCliente.DocIdentidadGub", "Documento de Identidad must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(nombreApellidos))
        {
            return Result.Failure(Error.Validation("PacienteCliente.NombreApellidos", "Nombre y Apellidos is required."));
        }

        if (nombreApellidos.Length > 200)
        {
            return Result.Failure(Error.Validation("PacienteCliente.NombreApellidos", "Nombre y Apellidos must not exceed 200 characters."));
        }

        if (fechaNacimiento == default)
        {
            return Result.Failure(Error.Validation("PacienteCliente.FechaNacimiento", "Fecha de Nacimiento is required."));
        }

        if (fechaNacimiento > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Result.Failure(Error.Validation("PacienteCliente.FechaNacimiento", "Fecha de Nacimiento cannot be in the future."));
        }

        if (contactoPrimario != null && contactoPrimario.Length > 100)
        {
            return Result.Failure(Error.Validation("PacienteCliente.ContactoPrimario", "Contacto Primario must not exceed 100 characters."));
        }

        DocIdentidadGub = docIdentidadGub;
        NombreApellidos = nombreApellidos;
        FechaNacimiento = fechaNacimiento;
        ContactoPrimario = contactoPrimario;

        return Result.Success();
    }
}
