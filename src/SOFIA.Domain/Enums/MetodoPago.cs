using System.ComponentModel;

namespace SOFIA.Domain.Enums;

public enum MetodoPago
{
    [Description("EFECTIVO")] Efectivo,
    [Description("YAPE_PLIN")] YapePlin,
    [Description("TARJETA")] Tarjeta,
    [Description("TRANSFERENCIA")] Transferencia
}
