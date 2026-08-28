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
    public Venta? Venta { get; }
    public LoteInventario? Lote { get; }

    public static Result<DetalleVenta> Create(
        Guid loteId,
        decimal cantidad,
        decimal precioUnitario,
        decimal costoHistorico,
        Guid? recetaId = null)
    {
        if (loteId == Guid.Empty)
        {
            return Result.Failure<DetalleVenta>(Error.Validation("DetalleVenta.LoteId", "Lote ID is required."));
        }

        if (cantidad <= 0)
        {
            return Result.Failure<DetalleVenta>(Error.Validation("DetalleVenta.Cantidad", "Quantity must be greater than zero."));
        }

        if (precioUnitario < 0)
        {
            return Result.Failure<DetalleVenta>(Error.Validation("DetalleVenta.PrecioUnitario", "Price cannot be negative."));
        }

        return Result.Success(new DetalleVenta
        {
            LoteId = loteId,
            CantidadVendida = cantidad,
            PrecioFijadoUnidad = precioUnitario,
            CostoUnitarioHistorico = costoHistorico,
            RecetaId = recetaId
        });
    }

    internal void SetVentaId(Guid ventaId) => VentaId = ventaId;
}
