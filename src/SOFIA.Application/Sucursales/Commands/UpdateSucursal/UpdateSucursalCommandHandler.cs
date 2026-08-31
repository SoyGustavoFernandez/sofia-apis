using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Commands.UpdateSucursal;

public class UpdateSucursalCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateSucursalCommand, Result>
{
    public async Task<Result> Handle(UpdateSucursalCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Sucursales
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Sucursal.NotFound", $"Sucursal with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(
            request.Nombre,
            request.DireccionFisica,
            request.NumeroLicencia,
            request.GerenteId);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
