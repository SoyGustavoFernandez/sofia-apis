using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class AuditoriaEventoSeguridad : BaseEntity
{
    private AuditoriaEventoSeguridad() { }

    public Guid EmpleadoId { get; private set; }
    public string TablaAfectada { get; private set; } = string.Empty;
    public Guid RegistroIdAfectado { get; private set; }
    public string TipoAccion { get; private set; } = string.Empty;
    public string? PayloadAnterior { get; private set; }
    public string? PayloadNuevo { get; private set; }
    public DateTime FechaHoraEvento { get; private set; }
    public string? DireccionIp { get; private set; }

    // Navigation Properties
    public Empleado? Empleado { get; }

    public static Result<AuditoriaEventoSeguridad> Create(
        Guid empleadoId,
        string tablaAfectada,
        Guid registroIdAfectado,
        string tipoAccion,
        string? payloadAnterior,
        string? payloadNuevo,
        string? direccionIp,
        DateTime? fechaHoraEvento = null)
    {
        if (empleadoId == Guid.Empty)
        {
            return Result.Failure<AuditoriaEventoSeguridad>(Error.Validation("AuditoriaEventoSeguridad.EmpleadoId", "Empleado ID is required."));
        }

        if (string.IsNullOrWhiteSpace(tablaAfectada))
        {
            return Result.Failure<AuditoriaEventoSeguridad>(Error.Validation("AuditoriaEventoSeguridad.TablaAfectada", "Tabla Afectada is required."));
        }

        if (tablaAfectada.Length > 50)
        {
            return Result.Failure<AuditoriaEventoSeguridad>(Error.Validation("AuditoriaEventoSeguridad.TablaAfectada", "Tabla Afectada must not exceed 50 characters."));
        }

        if (registroIdAfectado == Guid.Empty)
        {
            return Result.Failure<AuditoriaEventoSeguridad>(Error.Validation("AuditoriaEventoSeguridad.RegistroIdAfectado", "Registro ID Afectado is required."));
        }

        if (string.IsNullOrWhiteSpace(tipoAccion))
        {
            return Result.Failure<AuditoriaEventoSeguridad>(Error.Validation("AuditoriaEventoSeguridad.TipoAccion", "Tipo Acción is required."));
        }

        if (tipoAccion.Length > 20)
        {
            return Result.Failure<AuditoriaEventoSeguridad>(Error.Validation("AuditoriaEventoSeguridad.TipoAccion", "Tipo Acción must not exceed 20 characters."));
        }

        if (direccionIp != null && direccionIp.Length > 45)
        {
            return Result.Failure<AuditoriaEventoSeguridad>(Error.Validation("AuditoriaEventoSeguridad.DireccionIp", "Dirección IP must not exceed 45 characters."));
        }

        return Result.Success(new AuditoriaEventoSeguridad
        {
            EmpleadoId = empleadoId,
            TablaAfectada = tablaAfectada,
            RegistroIdAfectado = registroIdAfectado,
            TipoAccion = tipoAccion,
            PayloadAnterior = payloadAnterior,
            PayloadNuevo = payloadNuevo,
            FechaHoraEvento = fechaHoraEvento ?? DateTime.UtcNow,
            DireccionIp = direccionIp
        });
    }

    public Result Update(
        Guid empleadoId,
        string tablaAfectada,
        Guid registroIdAfectado,
        string tipoAccion,
        string? payloadAnterior,
        string? payloadNuevo,
        string? direccionIp)
    {
        if (empleadoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("AuditoriaEventoSeguridad.EmpleadoId", "Empleado ID is required."));
        }

        if (string.IsNullOrWhiteSpace(tablaAfectada))
        {
            return Result.Failure(Error.Validation("AuditoriaEventoSeguridad.TablaAfectada", "Tabla Afectada is required."));
        }

        if (tablaAfectada.Length > 50)
        {
            return Result.Failure(Error.Validation("AuditoriaEventoSeguridad.TablaAfectada", "Tabla Afectada must not exceed 50 characters."));
        }

        if (registroIdAfectado == Guid.Empty)
        {
            return Result.Failure(Error.Validation("AuditoriaEventoSeguridad.RegistroIdAfectado", "Registro ID Afectado is required."));
        }

        if (string.IsNullOrWhiteSpace(tipoAccion))
        {
            return Result.Failure(Error.Validation("AuditoriaEventoSeguridad.TipoAccion", "Tipo Acción is required."));
        }

        if (tipoAccion.Length > 20)
        {
            return Result.Failure(Error.Validation("AuditoriaEventoSeguridad.TipoAccion", "Tipo Acción must not exceed 20 characters."));
        }

        if (direccionIp != null && direccionIp.Length > 45)
        {
            return Result.Failure(Error.Validation("AuditoriaEventoSeguridad.DireccionIp", "Dirección IP must not exceed 45 characters."));
        }

        EmpleadoId = empleadoId;
        TablaAfectada = tablaAfectada;
        RegistroIdAfectado = registroIdAfectado;
        TipoAccion = tipoAccion;
        PayloadAnterior = payloadAnterior;
        PayloadNuevo = payloadNuevo;
        DireccionIp = direccionIp;

        return Result.Success();
    }
}
