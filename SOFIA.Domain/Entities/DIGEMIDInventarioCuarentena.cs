using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class DIGEMIDInventarioCuarentena : BaseEntity
{
    private DIGEMIDInventarioCuarentena() { }

    public Guid SucursalId { get; private set; }
    public Guid LoteId { get; private set; }
    public Guid? DetalleDevId { get; private set; }
    public decimal CantidadAislada { get; private set; }
    public string MotivoAislamiento { get; private set; } = string.Empty;
    public DateTime FechaIngresoCuarentena { get; private set; }
    public string EstadoResolucion { get; private set; } = string.Empty;
    public Guid EmpleadoRegistraId { get; private set; }

    // Navigation Properties
    public Sucursal? Sucursal { get; private set; }
    public LoteInventario? Lote { get; private set; }
    public Empleado? EmpleadoRegistra { get; private set; }

    public static Result<DIGEMIDInventarioCuarentena> Create(
        Guid sucursalId,
        Guid loteId,
        Guid? detalleDevId,
        decimal cantidadAislada,
        string motivoAislamiento,
        string estadoResolucion,
        Guid empleadoRegistraId,
        DateTime? fechaIngresoCuarentena = null)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.SucursalId", "Sucursal ID is required."));
        }

        if (loteId == Guid.Empty)
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.LoteId", "Lote ID is required."));
        }

        if (empleadoRegistraId == Guid.Empty)
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.EmpleadoRegistraId", "Empleado Registra ID is required."));
        }

        if (cantidadAislada <= 0)
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.CantidadAislada", "Cantidad Aislada must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(motivoAislamiento))
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.MotivoAislamiento", "Motivo Aislamiento is required."));
        }

        if (motivoAislamiento.Length > 50)
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.MotivoAislamiento", "Motivo Aislamiento must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(estadoResolucion))
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.EstadoResolucion", "Estado Resolucion is required."));
        }

        if (estadoResolucion.Length > 20)
        {
            return Result.Failure<DIGEMIDInventarioCuarentena>(Error.Validation("DIGEMIDInventarioCuarentena.EstadoResolucion", "Estado Resolucion must not exceed 20 characters."));
        }

        return Result.Success(new DIGEMIDInventarioCuarentena
        {
            SucursalId = sucursalId,
            LoteId = loteId,
            DetalleDevId = detalleDevId,
            CantidadAislada = cantidadAislada,
            MotivoAislamiento = motivoAislamiento,
            FechaIngresoCuarentena = fechaIngresoCuarentena ?? DateTime.UtcNow,
            EstadoResolucion = estadoResolucion,
            EmpleadoRegistraId = empleadoRegistraId
        });
    }

    public Result Update(
        Guid sucursalId,
        Guid loteId,
        Guid? detalleDevId,
        decimal cantidadAislada,
        string motivoAislamiento,
        string estadoResolucion,
        Guid empleadoRegistraId)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.SucursalId", "Sucursal ID is required."));
        }

        if (loteId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.LoteId", "Lote ID is required."));
        }

        if (empleadoRegistraId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.EmpleadoRegistraId", "Empleado Registra ID is required."));
        }

        if (cantidadAislada <= 0)
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.CantidadAislada", "Cantidad Aislada must be greater than zero."));
        }

        if (string.IsNullOrWhiteSpace(motivoAislamiento))
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.MotivoAislamiento", "Motivo Aislamiento is required."));
        }

        if (motivoAislamiento.Length > 50)
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.MotivoAislamiento", "Motivo Aislamiento must not exceed 50 characters."));
        }

        if (string.IsNullOrWhiteSpace(estadoResolucion))
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.EstadoResolucion", "Estado Resolucion is required."));
        }

        if (estadoResolucion.Length > 20)
        {
            return Result.Failure(Error.Validation("DIGEMIDInventarioCuarentena.EstadoResolucion", "Estado Resolucion must not exceed 20 characters."));
        }

        SucursalId = sucursalId;
        LoteId = loteId;
        DetalleDevId = detalleDevId;
        CantidadAislada = cantidadAislada;
        MotivoAislamiento = motivoAislamiento;
        EstadoResolucion = estadoResolucion;
        EmpleadoRegistraId = empleadoRegistraId;

        return Result.Success();
    }
}
