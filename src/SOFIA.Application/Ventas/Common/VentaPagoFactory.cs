using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

public static class VentaPagoFactory
{
    public static Result<List<VentaPago>> Build(List<CreateVentaPagoDto> dtos)
    {
        var pagos = new List<VentaPago>();

        foreach (var dto in dtos)
        {
            var pagoResult = VentaPago.Create(dto.MetodoPago, dto.MontoPagado, dto.ReferenciaOperacion, DateTime.UtcNow);
            if (!pagoResult.IsSuccess)
            {
                return Result.Failure<List<VentaPago>>(pagoResult.Error);
            }

            pagos.Add(pagoResult.Value);
        }

        return Result.Success(pagos);
    }
}
