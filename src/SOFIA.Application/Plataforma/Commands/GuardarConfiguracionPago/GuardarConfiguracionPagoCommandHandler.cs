using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Plataforma.Commands.GuardarConfiguracionPago;

public class GuardarConfiguracionPagoCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GuardarConfiguracionPagoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GuardarConfiguracionPagoCommand request, CancellationToken cancellationToken)
    {
        // Checked here too because this row is shared by every company
        if (!currentUser.EsAdminPlataforma)
        {
            return Result.Failure<Guid>(PlataformaAdminPolicy.RequiredError, 403);
        }

        var configuracion = await context.PlataformaConfiguracionesPago
            .IgnoreQueryFilters([QueryFilters.Tenant])
            .OrderBy(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (configuracion is null)
        {
            var creada = PlataformaConfiguracionPago.Create(request.WhatsappNumero, request.Instrucciones, request.YapeNumero);
            if (creada.IsFailure)
            {
                return Result.Failure<Guid>(creada.Error);
            }

            configuracion = creada.Value;
            _ = context.PlataformaConfiguracionesPago.Add(configuracion);
        }
        else
        {
            var actualizada = configuracion.Update(request.WhatsappNumero, request.Instrucciones, request.YapeNumero);
            if (actualizada.IsFailure)
            {
                return Result.Failure<Guid>(actualizada.Error);
            }
        }

        if (request.QrImagen is not null)
        {
            var qr = configuracion.CambiarQr(request.QrImagen);
            if (qr.IsFailure)
            {
                return Result.Failure<Guid>(qr.Error);
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success(configuracion.Id);
    }
}
