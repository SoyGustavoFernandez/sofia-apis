using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

public static class VentaRecetaVerificacion
{
    // Only whoever charges the sale can vouch for its unregistered prescriptions; when none is needed the client's flag is ignored and nothing is stored
    public static Result Registrar(Venta venta, bool requiereVerificacionReceta, bool recetaVerificada, Guid empleadoId)
    {
        if (!requiereVerificacionReceta)
        {
            return Result.Success();
        }

        return recetaVerificada
            ? venta.RegistrarVerificacionReceta(empleadoId, DateTime.UtcNow)
            : Result.Failure(Error.Validation("Venta.Receta.VerificacionRequerida", "Confirm that the prescription was verified before charging this sale."));
    }
}
