using System.ComponentModel;

namespace SOFIA.Domain.Enums;

public enum EstadoDespacho
{
    [Description("Preparando")] Preparando,
    [Description("En_Camino")] En_Camino,
    [Description("Entregado")] Entregado,
    [Description("Devuelto")] Devuelto
}
