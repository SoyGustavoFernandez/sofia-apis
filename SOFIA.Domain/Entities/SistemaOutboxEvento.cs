using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class SistemaOutboxEvento : BaseEntity
{
    private SistemaOutboxEvento() { }

    public string TipoEvento { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public DateTime FechaCreacion { get; private set; }
    public bool Procesado { get; private set; }
    public DateTime? FechaProcesamiento { get; private set; }
    public string? ErrorPublicacion { get; private set; }

    public static Result<SistemaOutboxEvento> Create(
        string tipoEvento,
        string payloadJson,
        bool procesado,
        DateTime? fechaProcesamiento,
        string? errorPublicacion,
        DateTime? fechaCreacion = null)
    {
        if (string.IsNullOrWhiteSpace(tipoEvento))
        {
            return Result.Failure<SistemaOutboxEvento>(Error.Validation("SistemaOutboxEvento.TipoEvento", "Tipo Evento is required."));
        }

        if (tipoEvento.Length > 100)
        {
            return Result.Failure<SistemaOutboxEvento>(Error.Validation("SistemaOutboxEvento.TipoEvento", "Tipo Evento must not exceed 100 characters."));
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Result.Failure<SistemaOutboxEvento>(Error.Validation("SistemaOutboxEvento.PayloadJson", "Payload JSON is required."));
        }

        return Result.Success(new SistemaOutboxEvento
        {
            TipoEvento = tipoEvento,
            PayloadJson = payloadJson,
            FechaCreacion = fechaCreacion ?? DateTime.UtcNow,
            Procesado = procesado,
            FechaProcesamiento = fechaProcesamiento,
            ErrorPublicacion = errorPublicacion
        });
    }

    public Result Update(
        string tipoEvento,
        string payloadJson,
        bool procesado,
        DateTime? fechaProcesamiento,
        string? errorPublicacion)
    {
        if (string.IsNullOrWhiteSpace(tipoEvento))
        {
            return Result.Failure(Error.Validation("SistemaOutboxEvento.TipoEvento", "Tipo Evento is required."));
        }

        if (tipoEvento.Length > 100)
        {
            return Result.Failure(Error.Validation("SistemaOutboxEvento.TipoEvento", "Tipo Evento must not exceed 100 characters."));
        }

        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return Result.Failure(Error.Validation("SistemaOutboxEvento.PayloadJson", "Payload JSON is required."));
        }

        TipoEvento = tipoEvento;
        PayloadJson = payloadJson;
        Procesado = procesado;
        FechaProcesamiento = fechaProcesamiento;
        ErrorPublicacion = errorPublicacion;

        return Result.Success();
    }
}
