using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class InventarioSucursal : BaseEntity
{
    private InventarioSucursal() { }

    public Guid SucursalId { get; private set; }
    public Guid LoteId { get; private set; }
    public decimal CantidadFisica { get; private set; }

    // Navigation Properties
    public Sucursal? Sucursal { get; private set; }
    public LoteInventario? Lote { get; private set; }

    public static Result<InventarioSucursal> Create(
        Guid sucursalId,
        Guid loteId,
        decimal cantidadFisica) => sucursalId == Guid.Empty
            ? Result.Failure<InventarioSucursal>(Error.Validation("InventarioSucursal.SucursalId", "Sucursal ID is required."))
            : loteId == Guid.Empty
                ? Result.Failure<InventarioSucursal>(Error.Validation("InventarioSucursal.LoteId", "Lote ID is required."))
                : cantidadFisica < 0
                    ? Result.Failure<InventarioSucursal>(Error.Validation("InventarioSucursal.CantidadFisica", "Quantity cannot be negative."))
                    : Result.Success(new InventarioSucursal
                    {
                        SucursalId = sucursalId,
                        LoteId = loteId,
                        CantidadFisica = cantidadFisica
                    });

    public void UpdateStock(decimal nuevaCantidad) => CantidadFisica = nuevaCantidad;

    public void AddStock(decimal cantidadAAgregar) => CantidadFisica += cantidadAAgregar;
}
