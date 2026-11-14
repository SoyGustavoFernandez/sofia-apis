using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Plataforma.Queries.GetConfiguracionPago;

public class GetConfiguracionPagoQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetConfiguracionPagoQuery, Result<ConfiguracionPagoDto>>
{
    public async Task<Result<ConfiguracionPagoDto>> Handle(GetConfiguracionPagoQuery request, CancellationToken cancellationToken)
    {
        // Read by blocked companies before login, so there is no tenant to filter by
        var configuracion = await context.PlataformaConfiguracionesPago
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (configuracion is null)
        {
            return Result.Success(new ConfiguracionPagoDto(null, null, null, null));
        }

        var qrDataUrl = configuracion.QrImagen is null
            ? null
            : $"data:{configuracion.QrContentType};base64,{Convert.ToBase64String(configuracion.QrImagen)}";

        return Result.Success(new ConfiguracionPagoDto(configuracion.WhatsappNumero, configuracion.YapeNumero, configuracion.Instrucciones, qrDataUrl));
    }
}
