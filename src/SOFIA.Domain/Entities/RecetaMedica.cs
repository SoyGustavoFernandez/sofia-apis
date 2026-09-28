using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed class RecetaMedica : BaseEntity
{
    private RecetaMedica() { } // Required for EF Core

    public Guid ClienteId { get; private set; }
    public Guid MedicoId { get; private set; }
    public DateOnly FechaExpedicion { get; private set; }
    public int RepeticionesMax { get; private set; }
    public string? IndicacionesUso { get; private set; }

    // Navigation Properties
    public PacienteCliente? Cliente { get; }
    public ProfesionalSalud? Medico { get; }

    public static Result<RecetaMedica> Create(
        Guid clienteId,
        Guid medicoId,
        DateOnly fechaExpedicion,
        int repeticionesMax = 0,
        string? indicacionesUso = null)
    {
        if (clienteId == Guid.Empty)
        {
            return Result.Failure<RecetaMedica>(Error.Validation("RecetaMedica.ClienteId", "Cliente ID is required."));
        }

        if (medicoId == Guid.Empty)
        {
            return Result.Failure<RecetaMedica>(Error.Validation("RecetaMedica.MedicoId", "Medico ID is required."));
        }

        if (fechaExpedicion == default)
        {
            return Result.Failure<RecetaMedica>(Error.Validation("RecetaMedica.FechaExpedicion", "Fecha de Expedición is required."));
        }

        if (fechaExpedicion > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Result.Failure<RecetaMedica>(Error.Validation("RecetaMedica.FechaExpedicion", "Fecha de Expedición cannot be in the future."));
        }

        if (repeticionesMax < 0)
        {
            return Result.Failure<RecetaMedica>(Error.Validation("RecetaMedica.RepeticionesMax", "Repeticiones Máximas must be greater than or equal to 0."));
        }

        return Result.Success(new RecetaMedica
        {
            ClienteId = clienteId,
            MedicoId = medicoId,
            FechaExpedicion = fechaExpedicion,
            RepeticionesMax = repeticionesMax,
            IndicacionesUso = indicacionesUso
        });
    }

    public Result Update(
        Guid clienteId,
        Guid medicoId,
        DateOnly fechaExpedicion,
        int repeticionesMax,
        string? indicacionesUso)
    {
        if (clienteId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("RecetaMedica.ClienteId", "Cliente ID is required."));
        }

        if (medicoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("RecetaMedica.MedicoId", "Medico ID is required."));
        }

        if (fechaExpedicion == default)
        {
            return Result.Failure(Error.Validation("RecetaMedica.FechaExpedicion", "Fecha de Expedición is required."));
        }

        if (fechaExpedicion > DateOnly.FromDateTime(DateTime.UtcNow))
        {
            return Result.Failure(Error.Validation("RecetaMedica.FechaExpedicion", "Fecha de Expedición cannot be in the future."));
        }

        if (repeticionesMax < 0)
        {
            return Result.Failure(Error.Validation("RecetaMedica.RepeticionesMax", "Repeticiones Máximas must be greater than or equal to 0."));
        }

        ClienteId = clienteId;
        MedicoId = medicoId;
        FechaExpedicion = fechaExpedicion;
        RepeticionesMax = repeticionesMax;
        IndicacionesUso = indicacionesUso;

        return Result.Success();
    }

    // Controlled prescriptions dispense once; a simple one allows the first dispensation plus RepeticionesMax refills
    public Result PuedeDispensar(int dispensacionesPrevias, CondicionVenta condicion)
    {
        var permitidas = condicion switch
        {
            CondicionVenta.VentaLibreOTC => int.MaxValue,
            CondicionVenta.RecetaSimple => 1 + RepeticionesMax,
            CondicionVenta.RecetaRetenida => 1,
            CondicionVenta.Estupefaciente => 1,
            _ => 1
        };

        return dispensacionesPrevias < permitidas
            ? Result.Success()
            : Result.Failure(Error.Validation("Venta.Receta.Agotada", "The prescription has no dispensations left."));
    }
}
