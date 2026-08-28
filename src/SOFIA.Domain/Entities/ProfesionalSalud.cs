using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class ProfesionalSalud : BaseEntity
{
    private ProfesionalSalud() { } // Required for EF Core

    public string NumeroRegistro { get; private set; } = string.Empty;
    public string NombrePrescriptor { get; private set; } = string.Empty;
    public string? DireccionClinica { get; private set; }

    public static Result<ProfesionalSalud> Create(
        string numeroRegistro,
        string nombrePrescriptor,
        string? direccionClinica)
    {
        if (string.IsNullOrWhiteSpace(numeroRegistro))
        {
            return Result.Failure<ProfesionalSalud>(Error.Validation("ProfesionalSalud.NumeroRegistro", "Número de Registro is required."));
        }

        if (numeroRegistro.Length > 50)
        {
            return Result.Failure<ProfesionalSalud>(Error.Validation("ProfesionalSalud.NumeroRegistro", "Número de Registro must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(nombrePrescriptor))
        {
            return Result.Failure<ProfesionalSalud>(Error.Validation("ProfesionalSalud.NombrePrescriptor", "Nombre de Prescriptor is required."));
        }

        if (nombrePrescriptor.Length > 150)
        {
            return Result.Failure<ProfesionalSalud>(Error.Validation("ProfesionalSalud.NombrePrescriptor", "Nombre de Prescriptor must not exceed 150 characters."));
        }

        if (direccionClinica != null && direccionClinica.Length > 255)
        {
            return Result.Failure<ProfesionalSalud>(Error.Validation("ProfesionalSalud.DireccionClinica", "Dirección de la Clínica must not exceed 255 characters."));
        }

        return Result.Success(new ProfesionalSalud
        {
            NumeroRegistro = numeroRegistro,
            NombrePrescriptor = nombrePrescriptor,
            DireccionClinica = direccionClinica
        });
    }

    public Result Update(
        string numeroRegistro,
        string nombrePrescriptor,
        string? direccionClinica)
    {
        if (string.IsNullOrWhiteSpace(numeroRegistro))
        {
            return Result.Failure(Error.Validation("ProfesionalSalud.NumeroRegistro", "Número de Registro is required."));
        }

        if (numeroRegistro.Length > 50)
        {
            return Result.Failure(Error.Validation("ProfesionalSalud.NumeroRegistro", "Número de Registro must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(nombrePrescriptor))
        {
            return Result.Failure(Error.Validation("ProfesionalSalud.NombrePrescriptor", "Nombre de Prescriptor is required."));
        }

        if (nombrePrescriptor.Length > 150)
        {
            return Result.Failure(Error.Validation("ProfesionalSalud.NombrePrescriptor", "Nombre de Prescriptor must not exceed 150 characters."));
        }

        if (direccionClinica != null && direccionClinica.Length > 255)
        {
            return Result.Failure(Error.Validation("ProfesionalSalud.DireccionClinica", "Dirección de la Clínica must not exceed 255 characters."));
        }

        NumeroRegistro = numeroRegistro;
        NombrePrescriptor = nombrePrescriptor;
        DireccionClinica = direccionClinica;

        return Result.Success();
    }
}
