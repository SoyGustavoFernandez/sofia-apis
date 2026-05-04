using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class FormulacionClinica : BaseEntity
{
    private FormulacionClinica() { } // Required for EF Core

    public Guid ProductoId { get; private set; }
    public Guid IngredienteId { get; private set; }
    public decimal ConcentracionDosis { get; private set; }
    public string UnidadDosisClinica { get; private set; } = string.Empty;
    public string? CodigoTeOrange { get; private set; }

    // Navigation Properties
    public Medicamento? Producto { get; private set; }
    public IngredienteActivo? Ingrediente { get; private set; }

    public static Result<FormulacionClinica> Create(
        Guid productoId,
        Guid ingredienteId,
        decimal concentracionDosis,
        string unidadDosisClinica,
        string? codigoTeOrange) =>
        productoId == Guid.Empty
            ? Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.ProductoId", "Producto ID is required."))
            : ingredienteId == Guid.Empty
            ? Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.IngredienteId", "Ingrediente ID is required."))
            : concentracionDosis <= 0
            ? Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.Concentracion", "Concentracion must be greater than zero."))
            : string.IsNullOrWhiteSpace(unidadDosisClinica)
            ? Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.UnidadDosis", "Unidad Dosis Clinica is required."))
            : Result.Success(new FormulacionClinica
            {
                ProductoId = productoId,
                IngredienteId = ingredienteId,
                ConcentracionDosis = concentracionDosis,
                UnidadDosisClinica = unidadDosisClinica,
                CodigoTeOrange = codigoTeOrange
            });

    public Result Update(
        Guid ingredienteId,
        decimal concentracionDosis,
        string unidadDosisClinica,
        string? codigoTeOrange)
    {
        if (ingredienteId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Formulacion.IngredienteId", "Ingrediente ID is required."));
        }

        if (concentracionDosis <= 0)
        {
            return Result.Failure(Error.Validation("Formulacion.Concentracion", "Concentracion must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(unidadDosisClinica))
        {
            return Result.Failure(Error.Validation("Formulacion.UnidadDosis", "Unidad Dosis Clinica is required."));
        }

        IngredienteId = ingredienteId;
        ConcentracionDosis = concentracionDosis;
        UnidadDosisClinica = unidadDosisClinica;
        CodigoTeOrange = codigoTeOrange;

        return Result.Success();
    }
}
