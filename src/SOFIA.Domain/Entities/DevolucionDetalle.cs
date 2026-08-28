using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DevolucionDetalle : BaseEntity
{
    private DevolucionDetalle() { } // Required for EF Core

    public Guid DevolucionId { get; private set; }
    public Guid DetalleVentaId { get; private set; }
    public decimal CantidadDevuelta { get; private set; }
    public Enums.DestinoDevolucion DestinoFisicoLogico { get; private set; }

    // Navigation Properties
    public DevolucionCabecera? Devolucion { get; }
    public DetalleVenta? DetalleVenta { get; }

    public static Result<DevolucionDetalle> Create(
        Guid detalleVentaId,
        decimal cantidadDevuelta,
        Enums.DestinoDevolucion destinoFisicoLogico)
    {
        if (detalleVentaId == Guid.Empty)
        {
            return Result.Failure<DevolucionDetalle>(Error.Validation("DevolucionDetalle.DetalleVentaId", "Detalle Venta ID is required."));
        }

        if (cantidadDevuelta <= 0)
        {
            return Result.Failure<DevolucionDetalle>(Error.Validation("DevolucionDetalle.CantidadDevuelta", "Cantidad devuelta must be greater than zero."));
        }

        return Result.Success(new DevolucionDetalle
        {
            DetalleVentaId = detalleVentaId,
            CantidadDevuelta = cantidadDevuelta,
            DestinoFisicoLogico = destinoFisicoLogico
        });
    }

    public Result Update(
        Guid detalleVentaId,
        decimal cantidadDevuelta,
        Enums.DestinoDevolucion destinoFisicoLogico)
    {
        if (detalleVentaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DevolucionDetalle.DetalleVentaId", "Detalle Venta ID is required."));
        }

        if (cantidadDevuelta <= 0)
        {
            return Result.Failure(Error.Validation("DevolucionDetalle.CantidadDevuelta", "Cantidad devuelta must be greater than zero."));
        }

        DetalleVentaId = detalleVentaId;
        CantidadDevuelta = cantidadDevuelta;
        DestinoFisicoLogico = destinoFisicoLogico;

        return Result.Success();
    }

    internal void SetDevolucionId(Guid devolucionId) => DevolucionId = devolucionId;
}
