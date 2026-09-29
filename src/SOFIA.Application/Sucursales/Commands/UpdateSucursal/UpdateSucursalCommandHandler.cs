using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var duplicado = await context.Sucursales
            .AnyAsync(s => s.Id != entity.Id && s.Numero_Licencia == request.NumeroLicencia && !s.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure(Error.Conflict("Sucursal.NumeroLicencia.Duplicado", "Another branch already uses this license number."), 409);
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
