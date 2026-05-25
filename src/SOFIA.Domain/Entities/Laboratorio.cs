using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class Laboratorio : BaseEntity
{
    private Laboratorio() { } // Required for EF Core

    public string NombreCompania { get; private set; } = string.Empty;
    public string? CodigoIdentificador { get; private set; }

    public static Result<Laboratorio> Create(string nombreCompania, string? codigoIdentificador) =>
        string.IsNullOrWhiteSpace(nombreCompania)
            ? Result.Failure<Laboratorio>(Error.Validation("Laboratorio.NombreCompania", "Nombre de Compañía is required."))
            : nombreCompania.Length > 150
            ? Result.Failure<Laboratorio>(Error.Validation("Laboratorio.NombreCompania", "Nombre de Compañía must not exceed 150 characters."))
            : codigoIdentificador != null && codigoIdentificador.Length > 50
            ? Result.Failure<Laboratorio>(Error.Validation("Laboratorio.CodigoIdentificador", "Código Identificador must not exceed 50 characters."))
            : Result.Success(new Laboratorio
            {
                NombreCompania = nombreCompania,
                CodigoIdentificador = codigoIdentificador
            });

    public Result Update(string nombreCompania, string? codigoIdentificador)
    {
        if (string.IsNullOrWhiteSpace(nombreCompania))
        {
            return Result.Failure(Error.Validation("Laboratorio.NombreCompania", "Nombre de Compañía is required."));
        }

        if (nombreCompania.Length > 150)
        {
            return Result.Failure(Error.Validation("Laboratorio.NombreCompania", "Nombre de Compañía must not exceed 150 characters."));
        }

        if (codigoIdentificador != null && codigoIdentificador.Length > 50)
        {
            return Result.Failure(Error.Validation("Laboratorio.CodigoIdentificador", "Código Identificador must not exceed 50 characters."));
        }

        NombreCompania = nombreCompania;
        CodigoIdentificador = codigoIdentificador;

        return Result.Success();
    }
}
