using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed class VentaPago : BaseEntity
{
    private VentaPago() { } // Required for EF Core

    public Guid TransaccionId { get; private set; }
    public MetodoPago MetodoPago { get; private set; }
    public decimal MontoPagado { get; private set; }
    public string? ReferenciaOperacion { get; private set; }
    public DateTime FechaPago { get; private set; }

    // Navigation Property
    public Venta? Venta { get; }

    public static Result<VentaPago> Create(
        MetodoPago metodoPago,
        decimal montoPagado,
        string? referenciaOperacion,
        DateTime fechaPago)
    {
        if (montoPagado <= 0.0m)
        {
            return Result.Failure<VentaPago>(Error.Validation("VentaPago.MontoPagado", "Monto Pagado must be greater than 0."));
        }

        if (RequiereReferencia(metodoPago) && string.IsNullOrWhiteSpace(referenciaOperacion))
        {
            return Result.Failure<VentaPago>(ReferenciaRequerida);
        }

        if (referenciaOperacion != null && referenciaOperacion.Length > 100)
        {
            return Result.Failure<VentaPago>(Error.Validation("VentaPago.ReferenciaOperacion", "Referencia de Operación must not exceed 100 characters."));
        }

        if (fechaPago == default)
        {
            return Result.Failure<VentaPago>(Error.Validation("VentaPago.FechaPago", "Fecha de Pago is required."));
        }

        return Result.Success(new VentaPago
        {
            MetodoPago = metodoPago,
            MontoPagado = montoPagado,
            ReferenciaOperacion = referenciaOperacion,
            FechaPago = fechaPago
        });
    }

    public Result Update(
        MetodoPago metodoPago,
        decimal montoPagado,
        string? referenciaOperacion,
        DateTime fechaPago)
    {
        if (montoPagado <= 0.0m)
        {
            return Result.Failure(Error.Validation("VentaPago.MontoPagado", "Monto Pagado must be greater than 0."));
        }

        if (RequiereReferencia(metodoPago) && string.IsNullOrWhiteSpace(referenciaOperacion))
        {
            return Result.Failure(ReferenciaRequerida);
        }

        if (referenciaOperacion != null && referenciaOperacion.Length > 100)
        {
            return Result.Failure(Error.Validation("VentaPago.ReferenciaOperacion", "Referencia de Operación must not exceed 100 characters."));
        }

        if (fechaPago == default)
        {
            return Result.Failure(Error.Validation("VentaPago.FechaPago", "Fecha de Pago is required."));
        }

        MetodoPago = metodoPago;
        MontoPagado = montoPagado;
        ReferenciaOperacion = referenciaOperacion;
        FechaPago = fechaPago;

        return Result.Success();
    }

    // Cash is counted in the drawer; every other method must be traceable to its operation number
    public static bool RequiereReferencia(MetodoPago metodoPago) => metodoPago != MetodoPago.Efectivo;

    private static Error ReferenciaRequerida => Error.Validation("Venta.Pagos.ReferenciaRequerida", "An operation reference is required for non-cash payments.");

    internal void SetVentaId(Guid ventaId) => TransaccionId = ventaId;
}
