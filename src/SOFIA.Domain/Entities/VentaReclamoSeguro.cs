using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class VentaReclamoSeguro : BaseEntity
{
    private VentaReclamoSeguro() { } // Required for EF Core

    public Guid DetalleVentaId { get; private set; }
    public Guid AseguradoraId { get; private set; }
    public decimal MontoCubierto { get; private set; }
    public decimal MontoCopagoPaciente { get; private set; }
    public string EstadoReclamo { get; private set; } = string.Empty;
    public string? CodigoAutorizacion { get; private set; }

    // Navigation Properties
    public DetalleVenta? DetalleVenta { get; }
    public AseguradoraMedica? Aseguradora { get; }

    public static Result<VentaReclamoSeguro> Create(
        Guid detalleVentaId,
        Guid aseguradoraId,
        decimal montoCubierto,
        decimal montoCopagoPaciente,
        string estadoReclamo,
        string? codigoAutorizacion)
    {
        if (detalleVentaId == Guid.Empty)
        {
            return Result.Failure<VentaReclamoSeguro>(Error.Validation("VentaReclamoSeguro.DetalleVentaId", "Detalle Venta ID is required."));
        }

        if (aseguradoraId == Guid.Empty)
        {
            return Result.Failure<VentaReclamoSeguro>(Error.Validation("VentaReclamoSeguro.AseguradoraId", "Aseguradora ID is required."));
        }

        if (montoCubierto < 0)
        {
            return Result.Failure<VentaReclamoSeguro>(Error.Validation("VentaReclamoSeguro.MontoCubierto", "Monto cubierto cannot be negative."));
        }

        if (montoCopagoPaciente < 0)
        {
            return Result.Failure<VentaReclamoSeguro>(Error.Validation("VentaReclamoSeguro.MontoCopagoPaciente", "Monto copago paciente cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(estadoReclamo))
        {
            return Result.Failure<VentaReclamoSeguro>(Error.Validation("VentaReclamoSeguro.EstadoReclamo", "Estado reclamo is required."));
        }

        if (estadoReclamo.Length > 50)
        {
            return Result.Failure<VentaReclamoSeguro>(Error.Validation("VentaReclamoSeguro.EstadoReclamo", "Estado reclamo must not exceed 50 characters."));
        }

        if (codigoAutorizacion != null && codigoAutorizacion.Length > 100)
        {
            return Result.Failure<VentaReclamoSeguro>(Error.Validation("VentaReclamoSeguro.CodigoAutorizacion", "Código autorización must not exceed 100 characters."));
        }

        return Result.Success(new VentaReclamoSeguro
        {
            DetalleVentaId = detalleVentaId,
            AseguradoraId = aseguradoraId,
            MontoCubierto = montoCubierto,
            MontoCopagoPaciente = montoCopagoPaciente,
            EstadoReclamo = estadoReclamo,
            CodigoAutorizacion = codigoAutorizacion
        });
    }

    public Result Update(
        Guid detalleVentaId,
        Guid aseguradoraId,
        decimal montoCubierto,
        decimal montoCopagoPaciente,
        string estadoReclamo,
        string? codigoAutorizacion)
    {
        if (detalleVentaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("VentaReclamoSeguro.DetalleVentaId", "Detalle Venta ID is required."));
        }

        if (aseguradoraId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("VentaReclamoSeguro.AseguradoraId", "Aseguradora ID is required."));
        }

        if (montoCubierto < 0)
        {
            return Result.Failure(Error.Validation("VentaReclamoSeguro.MontoCubierto", "Monto cubierto cannot be negative."));
        }

        if (montoCopagoPaciente < 0)
        {
            return Result.Failure(Error.Validation("VentaReclamoSeguro.MontoCopagoPaciente", "Monto copago paciente cannot be negative."));
        }

        if (string.IsNullOrWhiteSpace(estadoReclamo))
        {
            return Result.Failure(Error.Validation("VentaReclamoSeguro.EstadoReclamo", "Estado reclamo is required."));
        }

        if (estadoReclamo.Length > 50)
        {
            return Result.Failure(Error.Validation("VentaReclamoSeguro.EstadoReclamo", "Estado reclamo must not exceed 50 characters."));
        }

        if (codigoAutorizacion != null && codigoAutorizacion.Length > 100)
        {
            return Result.Failure(Error.Validation("VentaReclamoSeguro.CodigoAutorizacion", "Código autorización must not exceed 100 characters."));
        }

        DetalleVentaId = detalleVentaId;
        AseguradoraId = aseguradoraId;
        MontoCubierto = montoCubierto;
        MontoCopagoPaciente = montoCopagoPaciente;
        EstadoReclamo = estadoReclamo;
        CodigoAutorizacion = codigoAutorizacion;

        return Result.Success();
    }
}
