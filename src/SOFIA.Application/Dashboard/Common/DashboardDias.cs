namespace SOFIA.Application.Dashboard.Common;

public static class DashboardDias
{
    // The client sends the start of its local day; anything further from now is not a plausible "today" in any time zone
    public const int ToleranciaHoyHoras = 36;

    public const string HoyFueraDeRango = "Hoy must be the start of the current local day.";

    public static bool EsHoyValido(DateTimeOffset hoy, DateTimeOffset ahora) => Math.Abs((hoy - ahora).TotalHours) <= ToleranciaHoyHoras;

    public static VentanaDia Ventana(DateTimeOffset hoy) => Ventanas(hoy, 1)[0];

    // Consecutive local days ending today, oldest first; adjacent windows share their boundary instant without overlapping
    public static IReadOnlyList<VentanaDia> Ventanas(DateTimeOffset hoy, int dias)
    {
        var ventanas = new List<VentanaDia>(dias);
        for (var k = dias - 1; k >= 0; k--)
        {
            var desde = hoy.AddDays(-k);
            ventanas.Add(new VentanaDia(desde, desde.UtcDateTime, desde.AddDays(1).UtcDateTime));
        }

        return ventanas;
    }
}
