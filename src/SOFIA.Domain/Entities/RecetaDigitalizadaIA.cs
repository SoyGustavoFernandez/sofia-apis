using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class RecetaDigitalizadaIA : BaseEntity
{
    private readonly List<RegistroPrivacidadPresidio> _registrosPrivacidad = [];

    private RecetaDigitalizadaIA() { }

    public Guid RecetaId { get; private set; }
    public string RutaImagenBlob { get; private set; } = string.Empty;
    public string? TextoCrudoOcr { get; private set; }
    public string? EntidadesClinicasExtraidas { get; private set; }
    public decimal NivelConfianzaIa { get; private set; }
    public bool RequiereRevisionHumana { get; private set; }
    public DateTime FechaProcesamiento { get; private set; }

    // Navigation Properties
    public IReadOnlyCollection<RegistroPrivacidadPresidio> RegistrosPrivacidad => _registrosPrivacidad.AsReadOnly();

    public static Result<RecetaDigitalizadaIA> Create(
        Guid recetaId,
        string rutaImagenBlob,
        string? textoCrudoOcr,
        string? entidadesClinicasExtraidas,
        decimal nivelConfianzaIa,
        bool requiereRevisionHumana,
        DateTime? fechaProcesamiento = null)
    {
        if (recetaId == Guid.Empty)
        {
            return Result.Failure<RecetaDigitalizadaIA>(Error.Validation("RecetaDigitalizadaIA.RecetaId", "Receta ID is required."));
        }

        if (string.IsNullOrWhiteSpace(rutaImagenBlob))
        {
            return Result.Failure<RecetaDigitalizadaIA>(Error.Validation("RecetaDigitalizadaIA.RutaImagenBlob", "Ruta Imagen Blob is required."));
        }

        if (rutaImagenBlob.Length > 500)
        {
            return Result.Failure<RecetaDigitalizadaIA>(Error.Validation("RecetaDigitalizadaIA.RutaImagenBlob", "Ruta Imagen Blob must not exceed 500 characters."));
        }

        if (nivelConfianzaIa < 0 || nivelConfianzaIa > 100)
        {
            return Result.Failure<RecetaDigitalizadaIA>(Error.Validation("RecetaDigitalizadaIA.NivelConfianzaIa", "Nivel Confianza IA must be between 0 and 100."));
        }

        return Result.Success(new RecetaDigitalizadaIA
        {
            RecetaId = recetaId,
            RutaImagenBlob = rutaImagenBlob,
            TextoCrudoOcr = textoCrudoOcr,
            EntidadesClinicasExtraidas = entidadesClinicasExtraidas,
            NivelConfianzaIa = nivelConfianzaIa,
            RequiereRevisionHumana = requiereRevisionHumana,
            FechaProcesamiento = fechaProcesamiento ?? DateTime.UtcNow
        });
    }

    public Result Update(
        Guid recetaId,
        string rutaImagenBlob,
        string? textoCrudoOcr,
        string? entidadesClinicasExtraidas,
        decimal nivelConfianzaIa,
        bool requiereRevisionHumana)
    {
        if (recetaId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("RecetaDigitalizadaIA.RecetaId", "Receta ID is required."));
        }

        if (string.IsNullOrWhiteSpace(rutaImagenBlob))
        {
            return Result.Failure(Error.Validation("RecetaDigitalizadaIA.RutaImagenBlob", "Ruta Imagen Blob is required."));
        }

        if (rutaImagenBlob.Length > 500)
        {
            return Result.Failure(Error.Validation("RecetaDigitalizadaIA.RutaImagenBlob", "Ruta Imagen Blob must not exceed 500 characters."));
        }

        if (nivelConfianzaIa < 0 || nivelConfianzaIa > 100)
        {
            return Result.Failure(Error.Validation("RecetaDigitalizadaIA.NivelConfianzaIa", "Nivel Confianza IA must be between 0 and 100."));
        }

        RecetaId = recetaId;
        RutaImagenBlob = rutaImagenBlob;
        TextoCrudoOcr = textoCrudoOcr;
        EntidadesClinicasExtraidas = entidadesClinicasExtraidas;
        NivelConfianzaIa = nivelConfianzaIa;
        RequiereRevisionHumana = requiereRevisionHumana;

        return Result.Success();
    }
}
