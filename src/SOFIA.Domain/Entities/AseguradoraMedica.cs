using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class AseguradoraMedica : BaseEntity
{
    private readonly List<VentaReclamoSeguro> _reclamos = [];

    private AseguradoraMedica() { } // Required for EF Core

    public string NombreComercial { get; private set; } = string.Empty;
    public string CodigoIdentificadorNacional { get; private set; } = string.Empty;

    // Navigation Properties
    public IReadOnlyCollection<VentaReclamoSeguro> Reclamos => _reclamos.AsReadOnly();

    public static Result<AseguradoraMedica> Create(
        string nombreComercial,
        string codigoIdentificadorNacional) =>
        string.IsNullOrWhiteSpace(nombreComercial)
            ? Result.Failure<AseguradoraMedica>(Error.Validation("AseguradoraMedica.NombreComercial", "Nombre comercial is required."))
            : nombreComercial.Length > 150
            ? Result.Failure<AseguradoraMedica>(Error.Validation("AseguradoraMedica.NombreComercial", "Nombre comercial must not exceed 150 characters."))
            : string.IsNullOrWhiteSpace(codigoIdentificadorNacional)
            ? Result.Failure<AseguradoraMedica>(Error.Validation("AseguradoraMedica.CodigoIdentificadorNacional", "Código identificador nacional is required."))
            : codigoIdentificadorNacional.Length > 50
            ? Result.Failure<AseguradoraMedica>(Error.Validation("AseguradoraMedica.CodigoIdentificadorNacional", "Código identificador nacional must not exceed 50 characters."))
            : Result.Success(new AseguradoraMedica
            {
                NombreComercial = nombreComercial,
                CodigoIdentificadorNacional = codigoIdentificadorNacional
            });

    public Result Update(
        string nombreComercial,
        string codigoIdentificadorNacional)
    {
        if (string.IsNullOrWhiteSpace(nombreComercial))
        {
            return Result.Failure(Error.Validation("AseguradoraMedica.NombreComercial", "Nombre comercial is required."));
        }

        if (nombreComercial.Length > 150)
        {
            return Result.Failure(Error.Validation("AseguradoraMedica.NombreComercial", "Nombre comercial must not exceed 150 characters."));
        }

        if (string.IsNullOrWhiteSpace(codigoIdentificadorNacional))
        {
            return Result.Failure(Error.Validation("AseguradoraMedica.CodigoIdentificadorNacional", "Código identificador nacional is required."));
        }

        if (codigoIdentificadorNacional.Length > 50)
        {
            return Result.Failure(Error.Validation("AseguradoraMedica.CodigoIdentificadorNacional", "Código identificador nacional must not exceed 50 characters."));
        }

        NombreComercial = nombreComercial;
        CodigoIdentificadorNacional = codigoIdentificadorNacional;

        return Result.Success();
    }
}
