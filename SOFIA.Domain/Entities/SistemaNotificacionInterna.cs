using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class SistemaNotificacionInterna : BaseEntity
{
    private SistemaNotificacionInterna() { }

    public Guid EmpleadoOrigenId { get; private set; }
    public Guid? EmpleadoDestinoId { get; private set; }
    public Guid SucursalDestinoId { get; private set; }
    public string MensajeTexto { get; private set; } = string.Empty;
    public string? EntidadRelacionada { get; private set; }
    public Guid? EntidadId { get; private set; }
    public bool Leido { get; private set; }
    public DateTime? FechaLectura { get; private set; }

    // Navigation Properties
    public Empleado? EmpleadoOrigen { get; private set; }
    public Empleado? EmpleadoDestino { get; private set; }
    public Sucursal? SucursalDestino { get; private set; }

    public static Result<SistemaNotificacionInterna> Create(
        Guid empleadoOrigenId,
        Guid? empleadoDestinoId,
        Guid sucursalDestinoId,
        string mensajeTexto,
        string? entidadRelacionada,
        Guid? entidadId,
        bool leido,
        DateTime? fechaLectura)
    {
        if (empleadoOrigenId == Guid.Empty)
        {
            return Result.Failure<SistemaNotificacionInterna>(Error.Validation("SistemaNotificacionInterna.EmpleadoOrigenId", "Empleado Origen ID is required."));
        }

        if (sucursalDestinoId == Guid.Empty)
        {
            return Result.Failure<SistemaNotificacionInterna>(Error.Validation("SistemaNotificacionInterna.SucursalDestinoId", "Sucursal Destino ID is required."));
        }

        if (string.IsNullOrWhiteSpace(mensajeTexto))
        {
            return Result.Failure<SistemaNotificacionInterna>(Error.Validation("SistemaNotificacionInterna.MensajeTexto", "Mensaje Texto is required."));
        }

        if (mensajeTexto.Length > 500)
        {
            return Result.Failure<SistemaNotificacionInterna>(Error.Validation("SistemaNotificacionInterna.MensajeTexto", "Mensaje Texto must not exceed 500 characters."));
        }

        if (entidadRelacionada != null && entidadRelacionada.Length > 50)
        {
            return Result.Failure<SistemaNotificacionInterna>(Error.Validation("SistemaNotificacionInterna.EntidadRelacionada", "Entidad Relacionada must not exceed 50 characters."));
        }

        return Result.Success(new SistemaNotificacionInterna
        {
            EmpleadoOrigenId = empleadoOrigenId,
            EmpleadoDestinoId = empleadoDestinoId,
            SucursalDestinoId = sucursalDestinoId,
            MensajeTexto = mensajeTexto,
            EntidadRelacionada = entidadRelacionada,
            EntidadId = entidadId,
            Leido = leido,
            FechaLectura = fechaLectura
        });
    }

    public Result Update(
        Guid empleadoOrigenId,
        Guid? empleadoDestinoId,
        Guid sucursalDestinoId,
        string mensajeTexto,
        string? entidadRelacionada,
        Guid? entidadId,
        bool leido,
        DateTime? fechaLectura)
    {
        if (empleadoOrigenId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("SistemaNotificacionInterna.EmpleadoOrigenId", "Empleado Origen ID is required."));
        }

        if (sucursalDestinoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("SistemaNotificacionInterna.SucursalDestinoId", "Sucursal Destino ID is required."));
        }

        if (string.IsNullOrWhiteSpace(mensajeTexto))
        {
            return Result.Failure(Error.Validation("SistemaNotificacionInterna.MensajeTexto", "Mensaje Texto is required."));
        }

        if (mensajeTexto.Length > 500)
        {
            return Result.Failure(Error.Validation("SistemaNotificacionInterna.MensajeTexto", "Mensaje Texto must not exceed 500 characters."));
        }

        if (entidadRelacionada != null && entidadRelacionada.Length > 50)
        {
            return Result.Failure(Error.Validation("SistemaNotificacionInterna.EntidadRelacionada", "Entidad Relacionada must not exceed 50 characters."));
        }

        EmpleadoOrigenId = empleadoOrigenId;
        EmpleadoDestinoId = empleadoDestinoId;
        SucursalDestinoId = sucursalDestinoId;
        MensajeTexto = mensajeTexto;
        EntidadRelacionada = entidadRelacionada;
        EntidadId = entidadId;
        Leido = leido;
        FechaLectura = fechaLectura;

        return Result.Success();
    }

    public Result MarcarComoLeido()
    {
        if (Leido)
        {
            return Result.Success();
        }

        Leido = true;
        FechaLectura = DateTime.UtcNow;

        return Result.Success();
    }
}
