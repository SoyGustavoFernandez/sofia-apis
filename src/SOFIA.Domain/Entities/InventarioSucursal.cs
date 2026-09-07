using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class InventarioSucursal : BaseEntity
{
    private InventarioSucursal() { }

    public Guid SucursalId { get; private set; }
    public Guid LoteId { get; private set; }
    public decimal CantidadFisica { get; private set; }

    // Navigation Properties
    public Sucursal? Sucursal { get; }
    public LoteInventario? Lote { get; }

    public static Result<InventarioSucursal> Create(
        Guid sucursalId,
        Guid loteId,
        decimal cantidadFisica)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure<InventarioSucursal>(Error.Validation("InventarioSucursal.SucursalId", "Sucursal ID is required."));
        }

        if (loteId == Guid.Empty)
        {
            return Result.Failure<InventarioSucursal>(Error.Validation("InventarioSucursal.LoteId", "Lote ID is required."));
        }

        if (cantidadFisica < 0)
        {
            return Result.Failure<InventarioSucursal>(Error.Validation("InventarioSucursal.CantidadFisica", "Quantity cannot be negative."));
        }

        return Result.Success(new InventarioSucursal
        {
            SucursalId = sucursalId,
            LoteId = loteId,
            CantidadFisica = cantidadFisica
        });
    }

    public void UpdateStock(decimal nuevaCantidad) => CantidadFisica = nuevaCantidad;

    public void AddStock(decimal cantidadAAgregar) => CantidadFisica += cantidadAAgregar;

    // Manual inventory adjustment (physical count, shrinkage, correction).
    public Result AdjustStock(decimal nuevaCantidad)
    {
        if (nuevaCantidad < 0)
        {
            return Result.Failure(Error.Validation("InventarioSucursal.CantidadFisica", "Quantity cannot be negative."));
        }

        CantidadFisica = nuevaCantidad;
        return Result.Success();
    }
}
