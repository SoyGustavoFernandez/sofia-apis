using System.Text.RegularExpressions;

namespace SOFIA.Domain.Enums;

// SUNAT catálogo 06 codes, stored as text in the patient's Tipo_Doc_Identidad and the comprobante's Tipo_Doc_Identidad_Cliente
public static partial class TipoDocumentoIdentidad
{
    public const string Varios = "-";
    public const string Dni = "1";
    public const string CarneExtranjeria = "4";
    public const string Ruc = "6";
    public const string Pasaporte = "7";

    public static readonly IReadOnlyList<string> Validos = [Dni, CarneExtranjeria, Ruc, Pasaporte];

    // Short names accepted in bulk uploads and written in exports, keyed by catálogo 06 code
    public static readonly IReadOnlyDictionary<string, string> Abreviaturas = new Dictionary<string, string>
    {
        [Dni] = "DNI",
        [CarneExtranjeria] = "CE",
        [Ruc] = "RUC",
        [Pasaporte] = "PASAPORTE",
    };

    public static bool EsValido(string? tipo) => tipo is not null && Validos.Contains(tipo, StringComparer.Ordinal);

    public static bool EsNumeroValido(string? tipo, string? numero) => numero is not null && tipo switch
    {
        Dni => DniRegex().IsMatch(numero),
        Ruc => RucRegex().IsMatch(numero),
        CarneExtranjeria => CarneExtranjeriaRegex().IsMatch(numero),
        Pasaporte => PasaporteRegex().IsMatch(numero),
        _ => false,
    };

    // Accepts either the catálogo 06 code or its short name (case-insensitive); null when unknown
    public static string? Parse(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var texto = valor.Trim();
        return EsValido(texto)
            ? texto
            : Abreviaturas.FirstOrDefault(a => string.Equals(a.Value, texto, StringComparison.OrdinalIgnoreCase)).Key;
    }

    [GeneratedRegex(@"^\d{8}$")]
    private static partial Regex DniRegex();

    [GeneratedRegex(@"^\d{11}$")]
    private static partial Regex RucRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]{1,12}$")]
    private static partial Regex CarneExtranjeriaRegex();

    [GeneratedRegex(@"^[A-Za-z0-9]{1,20}$")]
    private static partial Regex PasaporteRegex();
}
