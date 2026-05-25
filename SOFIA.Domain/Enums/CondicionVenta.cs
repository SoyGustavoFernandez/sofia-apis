using System.ComponentModel;

namespace SOFIA.Domain.Enums;

public enum CondicionVenta
{
    [Description("Venta Libre (OTC)")] VentaLibreOTC,
    [Description("Receta Simple")] RecetaSimple,
    [Description("Receta Retenida")] RecetaRetenida,
    [Description("Estupefaciente")] Estupefaciente
}
