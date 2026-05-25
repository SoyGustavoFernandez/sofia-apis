using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class RegistroPrivacidadPresidio : BaseEntity
{
    private RegistroPrivacidadPresidio() { }

    public Guid ProcesamientoId { get; private set; }
    public string EntidadDetectada { get; private set; } = string.Empty;
    public byte[] TextoOriginalEncriptado { get; private set; } = [];
    public string TextoReemplazo { get; private set; } = string.Empty;
    public decimal? NivelRiesgoPii { get; private set; }
    public DateTime FechaAuditoria { get; private set; }

    // Navigation Properties
    public RecetaDigitalizadaIA? Procesamiento { get; private set; }

    public static Result<RegistroPrivacidadPresidio> Create(
        Guid procesamientoId,
        string entidadDetectada,
        byte[] textoOriginalEncriptado,
        string textoReemplazo,
        decimal? nivelRiesgoPii,
        DateTime? fechaAuditoria = null)
    {
        if (procesamientoId == Guid.Empty)
        {
            return Result.Failure<RegistroPrivacidadPresidio>(Error.Validation("RegistroPrivacidadPresidio.ProcesamientoId", "Procesamiento ID is required."));
        }

        if (string.IsNullOrWhiteSpace(entidadDetectada))
        {
            return Result.Failure<RegistroPrivacidadPresidio>(Error.Validation("RegistroPrivacidadPresidio.EntidadDetectada", "Entidad Detectada is required."));
        }

        if (entidadDetectada.Length > 50)
        {
            return Result.Failure<RegistroPrivacidadPresidio>(Error.Validation("RegistroPrivacidadPresidio.EntidadDetectada", "Entidad Detectada must not exceed 50 characters."));
        }

        if (textoOriginalEncriptado == null || textoOriginalEncriptado.Length == 0)
        {
            return Result.Failure<RegistroPrivacidadPresidio>(Error.Validation("RegistroPrivacidadPresidio.TextoOriginalEncriptado", "Texto Original Encriptado is required."));
        }

        if (string.IsNullOrWhiteSpace(textoReemplazo))
        {
            return Result.Failure<RegistroPrivacidadPresidio>(Error.Validation("RegistroPrivacidadPresidio.TextoReemplazo", "Texto Reemplazo is required."));
        }

        if (textoReemplazo.Length > 50)
        {
            return Result.Failure<RegistroPrivacidadPresidio>(Error.Validation("RegistroPrivacidadPresidio.TextoReemplazo", "Texto Reemplazo must not exceed 50 characters."));
        }

        if (nivelRiesgoPii.HasValue && (nivelRiesgoPii.Value < 0 || nivelRiesgoPii.Value > 100))
        {
            return Result.Failure<RegistroPrivacidadPresidio>(Error.Validation("RegistroPrivacidadPresidio.NivelRiesgoPii", "Nivel Riesgo PII must be between 0 and 100."));
        }

        return Result.Success(new RegistroPrivacidadPresidio
        {
            ProcesamientoId = procesamientoId,
            EntidadDetectada = entidadDetectada,
            TextoOriginalEncriptado = textoOriginalEncriptado,
            TextoReemplazo = textoReemplazo,
            NivelRiesgoPii = nivelRiesgoPii,
            FechaAuditoria = fechaAuditoria ?? DateTime.UtcNow
        });
    }

    public Result Update(
        Guid procesamientoId,
        string entidadDetectada,
        byte[] textoOriginalEncriptado,
        string textoReemplazo,
        decimal? nivelRiesgoPii)
    {
        if (procesamientoId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("RegistroPrivacidadPresidio.ProcesamientoId", "Procesamiento ID is required."));
        }

        if (string.IsNullOrWhiteSpace(entidadDetectada))
        {
            return Result.Failure(Error.Validation("RegistroPrivacidadPresidio.EntidadDetectada", "Entidad Detectada is required."));
        }

        if (entidadDetectada.Length > 50)
        {
            return Result.Failure(Error.Validation("RegistroPrivacidadPresidio.EntidadDetectada", "Entidad Detectada must not exceed 50 characters."));
        }

        if (textoOriginalEncriptado == null || textoOriginalEncriptado.Length == 0)
        {
            return Result.Failure(Error.Validation("RegistroPrivacidadPresidio.TextoOriginalEncriptado", "Texto Original Encriptado is required."));
        }

        if (string.IsNullOrWhiteSpace(textoReemplazo))
        {
            return Result.Failure(Error.Validation("RegistroPrivacidadPresidio.TextoReemplazo", "Texto Reemplazo is required."));
        }

        if (textoReemplazo.Length > 50)
        {
            return Result.Failure(Error.Validation("RegistroPrivacidadPresidio.TextoReemplazo", "Texto Reemplazo must not exceed 50 characters."));
        }

        if (nivelRiesgoPii.HasValue && (nivelRiesgoPii.Value < 0 || nivelRiesgoPii.Value > 100))
        {
            return Result.Failure(Error.Validation("RegistroPrivacidadPresidio.NivelRiesgoPii", "Nivel Riesgo PII must be between 0 and 100."));
        }

        ProcesamientoId = procesamientoId;
        EntidadDetectada = entidadDetectada;
        TextoOriginalEncriptado = textoOriginalEncriptado;
        TextoReemplazo = textoReemplazo;
        NivelRiesgoPii = nivelRiesgoPii;

        return Result.Success();
    }
}
