using System.Text.RegularExpressions;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

public sealed partial class SunatSerieFiscal : BaseEntity
{
    public const string EstadoActiva = "Activa";
    public const string EstadoInactiva = "Inactiva";

    // Document types a branch can configure series for
    public static readonly IReadOnlyList<TipoComprobante> TiposConfigurables =
        [TipoComprobante.Boleta, TipoComprobante.Factura, TipoComprobante.NotaCredito, TipoComprobante.Proforma];

    private SunatSerieFiscal() { }

    public Guid SucursalId { get; private set; }
    public TipoComprobante TipoComprobante { get; private set; }
    public string PrefijoSerie { get; private set; } = string.Empty;
    public int CorrelativoActual { get; private set; }
    public string EstadoSerie { get; private set; } = string.Empty;

    public bool EsActiva => EstadoSerie == EstadoActiva;

    // Navigation Properties
    public Sucursal? Sucursal { get; }

    public static bool EsEstadoValido(string? estado) => estado is EstadoActiva or EstadoInactiva;

    // SUNAT: 4 uppercase alphanumerics; boletas start with B, facturas with F, credit notes with the letter of the document they reverse
    public static bool EsPrefijoValido(TipoComprobante tipo, string? prefijo)
    {
        if (prefijo is null || !PrefijoRegex().IsMatch(prefijo))
        {
            return false;
        }

        return tipo switch
        {
            TipoComprobante.Boleta => prefijo[0] == 'B',
            TipoComprobante.Factura => prefijo[0] == 'F',
            TipoComprobante.NotaCredito => prefijo[0] is 'B' or 'F',
            TipoComprobante.Ticket or TipoComprobante.NotaDebito or TipoComprobante.Proforma => true,
            _ => false
        };
    }

    public static Result<SunatSerieFiscal> Create(
        Guid sucursalId,
        TipoComprobante tipoComprobante,
        string prefijoSerie,
        int correlativoActual,
        string estadoSerie)
    {
        var error = Validate(sucursalId, tipoComprobante, prefijoSerie, correlativoActual, estadoSerie);
        if (error is not null)
        {
            return Result.Failure<SunatSerieFiscal>(error);
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
        TipoComprobante tipoComprobante,
        string prefijoSerie,
        int correlativoActual,
        string estadoSerie)
    {
        var error = Validate(sucursalId, tipoComprobante, prefijoSerie, correlativoActual, estadoSerie);
        if (error is not null)
        {
            return Result.Failure(error);
        }

        SucursalId = sucursalId;
        TipoComprobante = tipoComprobante;
        PrefijoSerie = prefijoSerie;
        CorrelativoActual = correlativoActual;
        EstadoSerie = estadoSerie;

        return Result.Success();
    }

    // Once documents were issued, only the status may change: anything else would rewrite their fiscal numbers
    public bool CambiaNumeracion(Guid sucursalId, TipoComprobante tipoComprobante, string prefijoSerie, int correlativoActual) =>
        sucursalId != SucursalId || tipoComprobante != TipoComprobante || prefijoSerie != PrefijoSerie || correlativoActual != CorrelativoActual;

    private static Error? Validate(Guid sucursalId, TipoComprobante tipoComprobante, string prefijoSerie, int correlativoActual, string estadoSerie)
    {
        if (sucursalId == Guid.Empty)
        {
            return Error.Validation("SunatSerieFiscal.SucursalId", "Sucursal ID is required.");
        }

        if (!TiposConfigurables.Contains(tipoComprobante))
        {
            return Error.Validation("SunatSerieFiscal.TipoComprobante", "Unsupported document type for a series.");
        }

        if (!EsPrefijoValido(tipoComprobante, prefijoSerie))
        {
            return Error.Validation("SunatSerieFiscal.PrefijoSerie", "Prefijo Serie must be 4 uppercase alphanumerics with the letter required by the document type.");
        }

        if (correlativoActual < 0)
        {
            return Error.Validation("SunatSerieFiscal.CorrelativoActual", "Correlativo Actual must be greater than or equal to zero.");
        }

        if (!EsEstadoValido(estadoSerie))
        {
            return Error.Validation("SunatSerieFiscal.EstadoSerie", "Estado Serie must be Activa or Inactiva.");
        }

        return null;
    }

    [GeneratedRegex("^[A-Z0-9]{4}$")]
    private static partial Regex PrefijoRegex();
}
