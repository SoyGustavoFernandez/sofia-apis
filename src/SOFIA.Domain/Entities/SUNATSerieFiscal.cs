using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class SunatSerieFiscal : BaseEntity
{
    private SunatSerieFiscal() { }

    public Guid SucursalId { get; private set; }
    public Enums.TipoComprobante TipoComprobante { get; private set; }
    public string PrefijoSerie { get; private set; } = string.Empty;
    public int CorrelativoActual { get; private set; }
    public string EstadoSerie { get; private set; } = string.Empty;

    // Navigation Properties
    public Sucursal? Sucursal { get; }

    public static Result<SunatSerieFiscal> Create(
        Guid sucursalId,
        Enums.TipoComprobante tipoComprobante,
        string prefijoSerie,
        int correlativoActual,
        string estadoSerie)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure<SunatSerieFiscal>(Error.Validation("SunatSerieFiscal.SucursalId", "Sucursal ID is required."));
        }

        if (string.IsNullOrWhiteSpace(prefijoSerie))
        {
            return Result.Failure<SunatSerieFiscal>(Error.Validation("SunatSerieFiscal.PrefijoSerie", "Prefijo Serie is required."));
        }

        if (prefijoSerie.Length > 4)
        {
            return Result.Failure<SunatSerieFiscal>(Error.Validation("SunatSerieFiscal.PrefijoSerie", "Prefijo Serie must not exceed 4 characters."));
        }

        if (correlativoActual < 0)
        {
            return Result.Failure<SunatSerieFiscal>(Error.Validation("SunatSerieFiscal.CorrelativoActual", "Correlativo Actual must be greater than or equal to zero."));
        }

        if (string.IsNullOrWhiteSpace(estadoSerie))
        {
            return Result.Failure<SunatSerieFiscal>(Error.Validation("SunatSerieFiscal.EstadoSerie", "Estado Serie is required."));
        }

        if (estadoSerie.Length > 10)
        {
            return Result.Failure<SunatSerieFiscal>(Error.Validation("SunatSerieFiscal.EstadoSerie", "Estado Serie must not exceed 10 characters."));
        }

        return Result.Success(new SunatSerieFiscal
        {
            SucursalId = sucursalId,
            TipoComprobante = tipoComprobante,
            PrefijoSerie = prefijoSerie,
            CorrelativoActual = correlativoActual,
            EstadoSerie = estadoSerie
        });
    }

    public Result Update(
        Guid sucursalId,
        Enums.TipoComprobante tipoComprobante,
        string prefijoSerie,
        int correlativoActual,
        string estadoSerie)
    {
        if (sucursalId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("SunatSerieFiscal.SucursalId", "Sucursal ID is required."));
        }

        if (string.IsNullOrWhiteSpace(prefijoSerie))
        {
            return Result.Failure(Error.Validation("SunatSerieFiscal.PrefijoSerie", "Prefijo Serie is required."));
        }

        if (prefijoSerie.Length > 4)
        {
            return Result.Failure(Error.Validation("SunatSerieFiscal.PrefijoSerie", "Prefijo Serie must not exceed 4 characters."));
        }

        if (correlativoActual < 0)
        {
            return Result.Failure(Error.Validation("SunatSerieFiscal.CorrelativoActual", "Correlativo Actual must be greater than or equal to zero."));
        }

        if (string.IsNullOrWhiteSpace(estadoSerie))
        {
            return Result.Failure(Error.Validation("SunatSerieFiscal.EstadoSerie", "Estado Serie is required."));
        }

        if (estadoSerie.Length > 10)
        {
            return Result.Failure(Error.Validation("SunatSerieFiscal.EstadoSerie", "Estado Serie must not exceed 10 characters."));
        }

        SucursalId = sucursalId;
        TipoComprobante = tipoComprobante;
        PrefijoSerie = prefijoSerie;
        CorrelativoActual = correlativoActual;
        EstadoSerie = estadoSerie;

        return Result.Success();
    }
}
