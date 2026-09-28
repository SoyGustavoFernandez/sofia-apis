namespace SOFIA.Domain.Enums;

// Stored as text in Estado_Resolucion; the closed set keeps the sale-blocking "Retenido" check from being bypassed by typos
public static class EstadoResolucionCuarentena
{
    public const string Retenido = "Retenido";
    public const string Liberado = "Liberado";
    public const string Destruido = "Destruido";
    public const string Devuelto = "Devuelto";

    public static readonly IReadOnlyList<string> Validos = [Retenido, Liberado, Destruido, Devuelto];

    public static bool EsValido(string? estado) => estado is not null && Validos.Contains(estado, StringComparer.Ordinal);
}
