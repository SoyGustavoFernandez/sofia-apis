using System.ComponentModel;

namespace SOFIA.Domain.Enums;

public enum EstadoSesion
{
    [Description("Abierta")] Abierta,
    [Description("Cerrada")] Cerrada,
    [Description("Cuadrada")] Cuadrada
}
