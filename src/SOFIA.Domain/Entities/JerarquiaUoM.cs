using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class JerarquiaUoM : BaseEntity
{
    private JerarquiaUoM() { } // Required for EF Core

    public Guid ProductoId { get; private set; }
    public Guid UnidadMayorId { get; private set; }
    public Guid UnidadMenorId { get; private set; }
    public decimal Multiplicador { get; private set; }

    // Navigation Properties
    public Medicamento? Producto { get; private set; }
    public UnidadMedida? UnidadMayor { get; private set; }
    public UnidadMedida? UnidadMenor { get; private set; }

    public static Result<JerarquiaUoM> Create(
        Guid productoId,
        Guid unidadMayorId,
        Guid unidadMenorId,
        decimal multiplicador) =>
        productoId == Guid.Empty
            ? Result.Failure<JerarquiaUoM>(Error.Validation("JerarquiaUoM.ProductoId", "Producto ID is required."))
            : unidadMayorId == Guid.Empty
            ? Result.Failure<JerarquiaUoM>(Error.Validation("JerarquiaUoM.UnidadMayorId", "Unidad Mayor ID is required."))
            : unidadMenorId == Guid.Empty
            ? Result.Failure<JerarquiaUoM>(Error.Validation("JerarquiaUoM.UnidadMenorId", "Unidad Menor ID is required."))
            : unidadMayorId == unidadMenorId
            ? Result.Failure<JerarquiaUoM>(Error.Validation("JerarquiaUoM.Units", "Unidad Mayor and Unidad Menor cannot be the same."))
            : multiplicador <= 0
            ? Result.Failure<JerarquiaUoM>(Error.Validation("JerarquiaUoM.Multiplicador", "Multiplicador must be greater than zero."))
            : Result.Success(new JerarquiaUoM
            {
                ProductoId = productoId,
                UnidadMayorId = unidadMayorId,
                UnidadMenorId = unidadMenorId,
                Multiplicador = multiplicador
            });

    public Result Update(
        Guid productoId,
        Guid unidadMayorId,
        Guid unidadMenorId,
        decimal multiplicador)
    {
        if (productoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("JerarquiaUoM.ProductoId", "Producto ID is required."));
        }

        if (unidadMayorId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("JerarquiaUoM.UnidadMayorId", "Unidad Mayor ID is required."));
        }

        if (unidadMenorId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("JerarquiaUoM.UnidadMenorId", "Unidad Menor ID is required."));
        }

        if (unidadMayorId == unidadMenorId)
        {
            return Result.Failure(Error.Validation("JerarquiaUoM.Units", "Unidad Mayor and Unidad Menor cannot be the same."));
        }

        if (multiplicador <= 0)
        {
            return Result.Failure(Error.Validation("JerarquiaUoM.Multiplicador", "Multiplicador must be greater than zero."));
        }

        ProductoId = productoId;
        UnidadMayorId = unidadMayorId;
        UnidadMenorId = unidadMenorId;
        Multiplicador = multiplicador;

        return Result.Success();
    }
}
