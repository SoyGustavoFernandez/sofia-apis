using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class PosSesionCaja : BaseEntity
{
    private PosSesionCaja() { } // Required for EF Core

    public Guid SucursalId { get; private set; }
    public Guid EmpleadoId { get; private set; }
    public DateTime FechaHoraApertura { get; private set; }
    public DateTime? FechaHoraCierre { get; private set; }
    public decimal MontoAperturaEfectivo { get; private set; }
    public decimal? MontoCierreCalculado { get; private set; }
    public decimal? MontoCierreDeclarado { get; private set; }
    public decimal? DiferenciaArqueo { get; private set; }
    public Enums.EstadoSesion EstadoSesion { get; private set; } = Enums.EstadoSesion.Abierta;

    // Navigation Properties
    public Sucursal? Sucursal { get; }
    public Empleado? Empleado { get; }

    public static Result<PosSesionCaja> Create(
        Guid sucursalId,
        Guid empleadoId,
        DateTime fechaHoraApertura,
        decimal montoAperturaEfectivo)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure<PosSesionCaja>(Error.Validation("PosSesionCaja.SucursalId", "Sucursal ID is required."));
        }

        if (empleadoId == Guid.Empty)
        {
            return Result.Failure<PosSesionCaja>(Error.Validation("PosSesionCaja.EmpleadoId", "Empleado ID is required."));
        }

        if (fechaHoraApertura == default)
        {
            return Result.Failure<PosSesionCaja>(Error.Validation("PosSesionCaja.FechaHoraApertura", "Fecha de Apertura is required."));
        }

        if (montoAperturaEfectivo < 0.0m)
        {
            return Result.Failure<PosSesionCaja>(Error.Validation("PosSesionCaja.MontoAperturaEfectivo", "Monto de Apertura must be greater than or equal to 0."));
        }

        return Result.Success(new PosSesionCaja
        {
            SucursalId = sucursalId,
            EmpleadoId = empleadoId,
            FechaHoraApertura = fechaHoraApertura,
            MontoAperturaEfectivo = montoAperturaEfectivo,
            EstadoSesion = Enums.EstadoSesion.Abierta
        });
    }

    public Result Update(
        Guid sucursalId,
        Guid empleadoId,
        DateTime fechaHoraApertura,
        DateTime? fechaHoraCierre,
        decimal montoAperturaEfectivo,
        decimal? montoCierreCalculado,
        decimal? montoCierreDeclarado,
        decimal? diferenciaArqueo,
        Enums.EstadoSesion estadoSesion)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.SucursalId", "Sucursal ID is required."));
        }

        if (empleadoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.EmpleadoId", "Empleado ID is required."));
        }

        if (fechaHoraApertura == default)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.FechaHoraApertura", "Fecha de Apertura is required."));
        }

        if (montoAperturaEfectivo < 0.0m)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.MontoAperturaEfectivo", "Monto de Apertura must be greater than or equal to 0."));
        }

        if (montoCierreCalculado.HasValue && montoCierreCalculado.Value < 0.0m)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.MontoCierreCalculado", "Monto de Cierre Calculado cannot be negative."));
        }

        if (montoCierreDeclarado.HasValue && montoCierreDeclarado.Value < 0.0m)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.MontoCierreDeclarado", "Monto de Cierre Declarado cannot be negative."));
        }

        if (fechaHoraCierre.HasValue && fechaHoraCierre.Value < fechaHoraApertura)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.FechaHoraCierre", "Fecha de Cierre cannot be earlier than Fecha de Apertura."));
        }

        SucursalId = sucursalId;
        EmpleadoId = empleadoId;
        FechaHoraApertura = fechaHoraApertura;
        FechaHoraCierre = fechaHoraCierre;
        MontoAperturaEfectivo = montoAperturaEfectivo;
        MontoCierreCalculado = montoCierreCalculado;
        MontoCierreDeclarado = montoCierreDeclarado;
        DiferenciaArqueo = diferenciaArqueo;
        EstadoSesion = estadoSesion;

        return Result.Success();
    }

    public Result Cerrar(DateTime fechaHoraCierre, decimal montoDeclarado, decimal montoCalculado)
    {
        if (EstadoSesion != Enums.EstadoSesion.Abierta)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.Cerrar", "Only open sessions can be closed."));
        }

        if (fechaHoraCierre < FechaHoraApertura)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.FechaHoraCierre", "Closing date/time cannot be before opening date/time."));
        }

        if (montoDeclarado < 0.0m)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.MontoCierreDeclarado", "Declared closing cash cannot be negative."));
        }

        if (montoCalculado < 0.0m)
        {
            return Result.Failure(Error.Validation("PosSesionCaja.MontoCierreCalculado", "Calculated closing cash cannot be negative."));
        }

        FechaHoraCierre = fechaHoraCierre;
        MontoCierreDeclarado = montoDeclarado;
        MontoCierreCalculado = montoCalculado;
        DiferenciaArqueo = montoDeclarado - montoCalculado;
        EstadoSesion = DiferenciaArqueo == 0.0m ? Enums.EstadoSesion.Cuadrada : Enums.EstadoSesion.Cerrada;

        return Result.Success();
    }
}
