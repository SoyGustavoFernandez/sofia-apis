using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class MagistralConsumoInsumo : BaseEntity
{
    private MagistralConsumoInsumo() { }

    public Guid OrdenProduccionId { get; private set; }
    public Guid LoteMateriaPrimaId { get; private set; }
    public decimal CantidadConsumida { get; private set; }
    public string UnidadMedida { get; private set; } = string.Empty;

    // Navigation Properties
    public MagistralOrdenProduccion? OrdenProduccion { get; }
    public LoteInventario? LoteMateriaPrima { get; }

    public static Result<MagistralConsumoInsumo> Create(
        Guid ordenProduccionId,
        Guid loteMateriaPrimaId,
        decimal cantidadConsumida,
        string unidadMedida)
    {
        if (ordenProduccionId == Guid.Empty)
        {
            return Result.Failure<MagistralConsumoInsumo>(Error.Validation("MagistralConsumoInsumo.OrdenProduccionId", "Orden Producción ID is required."));
        }

        if (loteMateriaPrimaId == Guid.Empty)
        {
            return Result.Failure<MagistralConsumoInsumo>(Error.Validation("MagistralConsumoInsumo.LoteMateriaPrimaId", "Lote Materia Prima ID is required."));
        }

        if (cantidadConsumida <= 0)
        {
            return Result.Failure<MagistralConsumoInsumo>(Error.Validation("MagistralConsumoInsumo.CantidadConsumida", "Cantidad Consumida must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(unidadMedida))
        {
            return Result.Failure<MagistralConsumoInsumo>(Error.Validation("MagistralConsumoInsumo.UnidadMedida", "Unidad Medida is required."));
        }

        if (unidadMedida.Length > 20)
        {
            return Result.Failure<MagistralConsumoInsumo>(Error.Validation("MagistralConsumoInsumo.UnidadMedida", "Unidad Medida must not exceed 20 characters."));
        }

        return Result.Success(new MagistralConsumoInsumo
        {
            OrdenProduccionId = ordenProduccionId,
            LoteMateriaPrimaId = loteMateriaPrimaId,
            CantidadConsumida = cantidadConsumida,
            UnidadMedida = unidadMedida
        });
    }

    public Result Update(
        Guid ordenProduccionId,
        Guid loteMateriaPrimaId,
        decimal cantidadConsumida,
        string unidadMedida)
    {
        if (ordenProduccionId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("MagistralConsumoInsumo.OrdenProduccionId", "Orden Producción ID is required."));
        }

        if (loteMateriaPrimaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("MagistralConsumoInsumo.LoteMateriaPrimaId", "Lote Materia Prima ID is required."));
        }

        if (cantidadConsumida <= 0)
        {
            return Result.Failure(Error.Validation("MagistralConsumoInsumo.CantidadConsumida", "Cantidad Consumida must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(unidadMedida))
        {
            return Result.Failure(Error.Validation("MagistralConsumoInsumo.UnidadMedida", "Unidad Medida is required."));
        }

        if (unidadMedida.Length > 20)
        {
            return Result.Failure(Error.Validation("MagistralConsumoInsumo.UnidadMedida", "Unidad Medida must not exceed 20 characters."));
        }

        OrdenProduccionId = ordenProduccionId;
        LoteMateriaPrimaId = loteMateriaPrimaId;
        CantidadConsumida = cantidadConsumida;
        UnidadMedida = unidadMedida;

        return Result.Success();
    }
}
