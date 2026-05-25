using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DetalleVenta : BaseEntity
{
    private DetalleVenta() { }

    public Guid VentaId { get; private set; }
    public Guid LoteId { get; private set; }
    public Guid? RecetaId { get; private set; }
    public decimal CantidadVendida { get; private set; }
    public decimal PrecioFijadoUnidad { get; private set; }
    public decimal CostoUnitarioHistorico { get; private set; }

    // Navigation Properties
    public Venta? Venta { get; private set; }
    public LoteInventario? Lote { get; private set; }

    public static Result<DetalleVenta> Create(
        Guid loteId,
        decimal cantidad,
        decimal precioUnitario,
        decimal costoHistorico,
        Guid? recetaId = null) =>
        loteId == Guid.Empty
            ? Result.Failure<DetalleVenta>(Error.Validation("DetalleVenta.LoteId", "Lote ID is required."))
            : cantidad <= 0
                ? Result.Failure<DetalleVenta>(Error.Validation("DetalleVenta.Cantidad", "Quantity must be greater than zero."))
                : precioUnitario < 0
                    ? Result.Failure<DetalleVenta>(Error.Validation("DetalleVenta.PrecioUnitario", "Price cannot be negative."))
                    : Result.Success(new DetalleVenta
                    {
                        LoteId = loteId,
                        CantidadVendida = cantidad,
                        PrecioFijadoUnidad = precioUnitario,
                        CostoUnitarioHistorico = costoHistorico,
                        RecetaId = recetaId
                    });

    internal void SetVentaId(Guid ventaId) => VentaId = ventaId;
}
