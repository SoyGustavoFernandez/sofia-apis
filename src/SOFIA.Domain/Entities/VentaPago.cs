using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class VentaPago : BaseEntity
{
    private VentaPago() { } // Required for EF Core

    public Guid TransaccionId { get; private set; }
    public string MetodoPago { get; private set; } = string.Empty;
    public decimal MontoPagado { get; private set; }
    public string? ReferenciaOperacion { get; private set; }
    public DateTime FechaPago { get; private set; }

    // Navigation Property
    public Venta? Venta { get; }

    public static Result<VentaPago> Create(
        Guid transaccionId,
        string metodoPago,
        decimal montoPagado,
        string? referenciaOperacion,
        DateTime fechaPago)
    {
        if (transaccionId == Guid.Empty)
        {
            return Result.Failure<VentaPago>(Error.Validation("VentaPago.TransaccionId", "Transacción ID is required."));
        }

        if (string.IsNullOrWhiteSpace(metodoPago))
        {
            return Result.Failure<VentaPago>(Error.Validation("VentaPago.MetodoPago", "Método de Pago is required."));
        }

        if (metodoPago.Length > 50)
        {
            return Result.Failure<VentaPago>(Error.Validation("VentaPago.MetodoPago", "Método de Pago must not exceed 50 characters."));
        }

        if (montoPagado <= 0.0m)
        {
            return Result.Failure<VentaPago>(Error.Validation("VentaPago.MontoPagado", "Monto Pagado must be greater than 0."));
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
            TransaccionId = transaccionId,
            MetodoPago = metodoPago,
            MontoPagado = montoPagado,
            ReferenciaOperacion = referenciaOperacion,
            FechaPago = fechaPago
        });
    }

    public Result Update(
        Guid transaccionId,
        string metodoPago,
        decimal montoPagado,
        string? referenciaOperacion,
        DateTime fechaPago)
    {
        if (transaccionId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("VentaPago.TransaccionId", "Transacción ID is required."));
        }

        if (string.IsNullOrWhiteSpace(metodoPago))
        {
            return Result.Failure(Error.Validation("VentaPago.MetodoPago", "Método de Pago is required."));
        }

        if (metodoPago.Length > 50)
        {
            return Result.Failure(Error.Validation("VentaPago.MetodoPago", "Método de Pago must not exceed 50 characters."));
        }

        if (montoPagado <= 0.0m)
        {
            return Result.Failure(Error.Validation("VentaPago.MontoPagado", "Monto Pagado must be greater than 0."));
        }

        if (referenciaOperacion != null && referenciaOperacion.Length > 100)
        {
            return Result.Failure(Error.Validation("VentaPago.ReferenciaOperacion", "Referencia de Operación must not exceed 100 characters."));
        }

        if (fechaPago == default)
        {
            return Result.Failure(Error.Validation("VentaPago.FechaPago", "Fecha de Pago is required."));
        }

        TransaccionId = transaccionId;
        MetodoPago = metodoPago;
        MontoPagado = montoPagado;
        ReferenciaOperacion = referenciaOperacion;
        FechaPago = fechaPago;

        return Result.Success();
    }
}
