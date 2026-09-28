using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.SeriesFiscales.Commands.DeleteSerieFiscal;

public class DeleteSerieFiscalCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteSerieFiscalCommand, Result>
{
    public async Task<Result> Handle(DeleteSerieFiscalCommand request, CancellationToken cancellationToken)
    {
        var serie = await context.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

        if (serie is null)
        {
            return Result.Failure(Error.NotFound("SerieFiscal.NotFound", "The specified fiscal series does not exist."), 404);
        }

        var tieneComprobantes = await context.SUNATComprobantesEmitidos
            .AnyAsync(c => c.SerieId == request.Id && !c.IsDeleted, cancellationToken);
        if (tieneComprobantes)
        {
            return Result.Failure(Error.Conflict("SerieFiscal.InUse", "Cannot delete a fiscal series that already issued documents."), 409);
        }

        _ = context.SUNATSeriesFiscales.Remove(serie);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
