using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class FormulacionClinica : BaseEntity
{
    private FormulacionClinica() { } // Required for EF Core

    public Guid ProductoId { get; private set; }
    public Guid IngredienteId { get; private set; }
    public decimal ConcentracionDosis { get; private set; }
    public Guid UnidadMedidaId { get; private set; }
    public string? CodigoTeOrange { get; private set; }

    // Navigation Properties
    public Medicamento? Producto { get; }
    public IngredienteActivo? Ingrediente { get; }
    public UnidadMedida? UnidadMedida { get; }

    public static Result<FormulacionClinica> Create(
        Guid productoId,
        Guid ingredienteId,
        decimal concentracionDosis,
        Guid unidadMedidaId,
        string? codigoTeOrange)
    {
        if (productoId == Guid.Empty)
        {
            return Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.ProductoId", "Producto ID is required."));
        }

        if (ingredienteId == Guid.Empty)
        {
            return Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.IngredienteId", "Ingrediente ID is required."));
        }

        if (concentracionDosis <= 0)
        {
            return Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.Concentracion", "Concentracion must be greater than zero."));
        }

        if (unidadMedidaId == Guid.Empty)
        {
            return Result.Failure<FormulacionClinica>(Error.Validation("Formulacion.UnidadMedidaId", "Unidad Medida ID is required."));
        }

        return Result.Success(new FormulacionClinica
        {
            ProductoId = productoId,
            IngredienteId = ingredienteId,
            ConcentracionDosis = concentracionDosis,
            UnidadMedidaId = unidadMedidaId,
            CodigoTeOrange = codigoTeOrange
        });
    }

    public Result Update(
        Guid ingredienteId,
        decimal concentracionDosis,
        Guid unidadMedidaId,
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

        if (unidadMedidaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Formulacion.UnidadMedidaId", "Unidad Medida ID is required."));
        }

        IngredienteId = ingredienteId;
        ConcentracionDosis = concentracionDosis;
        UnidadMedidaId = unidadMedidaId;
        CodigoTeOrange = codigoTeOrange;

        return Result.Success();
    }
}
