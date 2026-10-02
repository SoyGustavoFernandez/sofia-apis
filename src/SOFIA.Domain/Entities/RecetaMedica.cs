using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed class RecetaMedica : BaseEntity
{
    public const int NumeroRecetaMaxLength = 30;

    private RecetaMedica() { } // Required for EF Core

    public Guid ClienteId { get; private set; }
    public Guid MedicoId { get; private set; }
    public DateOnly FechaExpedicion { get; private set; }
    public int RepeticionesMax { get; private set; }
    public string? IndicacionesUso { get; private set; }

    // Optional in the master, but retained and narcotic products can only be sold against a prescription that has it
    public string? NumeroReceta { get; private set; }

    // Navigation Properties
    public PacienteCliente? Cliente { get; }
    public ProfesionalSalud? Medico { get; }

    public static Result<RecetaMedica> Create(
        Guid clienteId,
        Guid medicoId,
        DateOnly fechaExpedicion,
        int repeticionesMax = 0,
        string? indicacionesUso = null,
        string? numeroReceta = null)
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

        var numero = NormalizarNumero(numeroReceta);
        if (numero?.Length > NumeroRecetaMaxLength)
        {
            return Result.Failure<RecetaMedica>(NumeroRecetaDemasiadoLargo);
        }

        return Result.Success(new RecetaMedica
        {
            ClienteId = clienteId,
            MedicoId = medicoId,
            FechaExpedicion = fechaExpedicion,
            RepeticionesMax = repeticionesMax,
            IndicacionesUso = indicacionesUso,
            NumeroReceta = numero
        });
    }

    public Result Update(
        Guid clienteId,
        Guid medicoId,
        DateOnly fechaExpedicion,
        int repeticionesMax,
        string? indicacionesUso,
        string? numeroReceta)
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

        var numero = NormalizarNumero(numeroReceta);
        if (numero?.Length > NumeroRecetaMaxLength)
        {
            return Result.Failure(NumeroRecetaDemasiadoLargo);
        }

        ClienteId = clienteId;
        MedicoId = medicoId;
        FechaExpedicion = fechaExpedicion;
        RepeticionesMax = repeticionesMax;
        IndicacionesUso = indicacionesUso;
        NumeroReceta = numero;

        return Result.Success();
    }

    private static Error NumeroRecetaDemasiadoLargo => Error.Validation("RecetaMedica.NumeroReceta", $"Número de receta must not exceed {NumeroRecetaMaxLength} characters.");

    // A blank number is stored as no number
    private static string? NormalizarNumero(string? numeroReceta) => string.IsNullOrWhiteSpace(numeroReceta) ? null : numeroReceta.Trim();

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
