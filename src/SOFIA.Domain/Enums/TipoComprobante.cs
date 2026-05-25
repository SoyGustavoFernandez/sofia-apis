using System.ComponentModel;

namespace SOFIA.Domain.Enums;

public enum TipoComprobante
{
    [Description("00")] Ticket,
    [Description("01")] Factura,
    [Description("03")] Boleta,
    [Description("07")] NotaCredito,
    [Description("08")] NotaDebito
}
