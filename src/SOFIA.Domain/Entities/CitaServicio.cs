using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

// An appointment for a service at one branch; only a scheduled one can still change
public sealed class CitaServicio : BaseEntity
{
    // A few minutes of slack so "now" is still accepted while the form is being filled
    private static readonly TimeSpan Tolerancia = TimeSpan.FromMinutes(5);

    public static readonly Error YaCerradaError = Error.Conflict("CitaServicio.YaCerrada", "Only a scheduled appointment can change.");

    private CitaServicio() { }

    // Only a walk-in for a service that records nothing about the patient may lack one
    public Guid? PacienteId { get; private set; }
    public Guid ServicioId { get; private set; }
    public Guid SucursalId { get; private set; }
    public DateTimeOffset FechaHora { get; private set; }
    public EstadoCita Estado { get; private set; }
    public bool AceptaRecordatorio { get; private set; }
    public OrigenMedicamento? Origen { get; private set; }
    public Guid? LoteId { get; private set; }
    public string? MedicamentoTraido { get; private set; }
    public string? Dosis { get; private set; }
    public string? Resultado { get; private set; }
    public Guid? VentaPreviaId { get; private set; }
    public Guid? VentaId { get; private set; }
    public DateTimeOffset? FechaAtencion { get; private set; }

    public static Result<CitaServicio> Create(Guid pacienteId, Guid servicioId, Guid sucursalId, DateTimeOffset fechaHora, bool aceptaRecordatorio, DateTimeOffset ahora)
    {
        if (pacienteId == Guid.Empty || sucursalId == Guid.Empty)
        {
            return Result.Failure<CitaServicio>(Error.Validation("CitaServicio.PacienteId", "Patient and branch are required."));
        }

        var cita = new CitaServicio { PacienteId = pacienteId, SucursalId = sucursalId, Estado = EstadoCita.Programada };
        var result = cita.Reprogramar(servicioId, fechaHora, aceptaRecordatorio, ahora);
        return result.IsSuccess ? Result.Success(cita) : Result.Failure<CitaServicio>(result.Error);
    }

    public Result Reprogramar(Guid servicioId, DateTimeOffset fechaHora, bool aceptaRecordatorio, DateTimeOffset ahora)
    {
        if (Estado != EstadoCita.Programada)
        {
            return Result.Failure(YaCerradaError);
        }

        if (servicioId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("CitaServicio.ServicioId", "Service is required."));
        }

        if (fechaHora < ahora - Tolerancia)
        {
            return Result.Failure(Error.Validation("CitaServicio.FechaPasada", "The appointment cannot be in the past."));
        }

        ServicioId = servicioId;
        FechaHora = fechaHora;
        AceptaRecordatorio = aceptaRecordatorio;
        return Result.Success();
    }

    public static Result<CitaServicio> CrearSinCita(Guid? pacienteId, Guid servicioId, Guid sucursalId, DateTimeOffset ahora) => servicioId == Guid.Empty || sucursalId == Guid.Empty
            ? Result.Failure<CitaServicio>(Error.Validation("CitaServicio.ServicioId", "Service and branch are required."))
            : Result.Success(new CitaServicio { PacienteId = pacienteId, ServicioId = servicioId, SucursalId = sucursalId, FechaHora = ahora, Estado = EstadoCita.Programada });

    public Result Atender(ServicioCatalogo servicio, AtencionDatos datos, Guid ventaId, DateTimeOffset ahora)
    {
        if (Estado != EstadoCita.Programada)
        {
            return Result.Failure(YaCerradaError);
        }

        var validacion = Validar(servicio, datos);
        if (validacion.IsFailure)
        {
            return validacion;
        }

        var aplica = servicio.AplicaMedicamento;
        Origen = aplica ? datos.Origen : null;
        LoteId = aplica && datos.Origen != OrigenMedicamento.TraidoPorPaciente ? datos.LoteId : null;
        MedicamentoTraido = aplica && datos.Origen == OrigenMedicamento.TraidoPorPaciente ? datos.MedicamentoTraido?.Trim() : null;
        Dosis = aplica ? datos.Dosis?.Trim() : null;
        VentaPreviaId = aplica && datos.Origen == OrigenMedicamento.CompradoAntes ? datos.VentaPreviaId : null;
        Resultado = servicio.RegistraResultado ? datos.Resultado?.Trim() : null;
        VentaId = ventaId;
        FechaAtencion = ahora;
        Estado = EstadoCita.Atendida;
        return Result.Success();
    }

    // Voiding the charge reopens the appointment and keeps what was recorded; a walk-in without patient has nothing to reopen
    public void AnularCobro()
    {
        VentaId = null;
        Estado = PacienteId is null ? EstadoCita.Cancelada : EstadoCita.Programada;
    }

    private Result Validar(ServicioCatalogo servicio, AtencionDatos datos)
    {
        if (PacienteId is null && (servicio.AplicaMedicamento || servicio.RegistraResultado))
        {
            return Result.Failure(Error.Validation("Atencion.PacienteRequerido", "This service needs a registered patient."));
        }

        if (servicio.RegistraResultado && string.IsNullOrWhiteSpace(datos.Resultado))
        {
            return Result.Failure(Error.Validation("Atencion.ResultadoRequerido", "This service records a result."));
        }

        return servicio.AplicaMedicamento ? ValidarMedicamento(datos) : Result.Success();
    }

    private static Result ValidarMedicamento(AtencionDatos datos)
    {
        var completo = datos.Origen switch
        {
            OrigenMedicamento.CompradoAhora => datos.LoteId.HasValue,
            OrigenMedicamento.CompradoAntes => datos.LoteId.HasValue && datos.VentaPreviaId.HasValue,
            OrigenMedicamento.TraidoPorPaciente => !string.IsNullOrWhiteSpace(datos.MedicamentoTraido),
            _ => false
        };

        return completo && !string.IsNullOrWhiteSpace(datos.Dosis)
            ? Result.Success()
            : Result.Failure(Error.Validation("Atencion.MedicamentoRequerido", "This service needs the medicine, its origin and the dose."));
    }

    // Attended is only reached by performing and charging the service
    public Result Cerrar(EstadoCita estado)
    {
        if (Estado != EstadoCita.Programada)
        {
            return Result.Failure(YaCerradaError);
        }

        if (estado is not (EstadoCita.Cancelada or EstadoCita.NoAsistio))
        {
            return Result.Failure(Error.Validation("CitaServicio.Estado", "An appointment can only be cancelled or marked as missed."));
        }

        Estado = estado;
        return Result.Success();
    }
}
