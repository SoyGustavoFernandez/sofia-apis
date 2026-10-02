using MediatR;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetConfiguracionVenta;

public class GetConfiguracionVentaQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetConfiguracionVentaQuery, Result<ConfiguracionVentaDto>>
{
    public async Task<Result<ConfiguracionVentaDto>> Handle(GetConfiguracionVentaQuery request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<ConfiguracionVentaDto>(sucursalResult.Error);
        }

        var exigirRegistroReceta = await VentaDetalleFactory.ExigeRegistroRecetaAsync(context, sucursalResult.Value, cancellationToken);

        return Result.Success(new ConfiguracionVentaDto(exigirRegistroReceta));
    }
}
