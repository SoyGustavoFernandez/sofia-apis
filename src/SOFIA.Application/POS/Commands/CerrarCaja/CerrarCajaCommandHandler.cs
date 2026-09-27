using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.POS.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.POS.Commands.CerrarCaja;

public class CerrarCajaCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<CerrarCajaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CerrarCajaCommand request, CancellationToken cancellationToken)
    {
        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<Guid>(empleadoResult.Error);
        }

        var sesion = await dbContext.POSSesionesCaja.FirstOrDefaultAsync(x => x.Id == request.SesionId, cancellationToken);
        if (sesion == null)
        {
            return Result.Failure<Guid>(Error.NotFound("Caja", "Sesion no encontrada"));
        }

        // Only the cashier who opened the drawer, or an Admin, can count and close it
        if (sesion.EmpleadoId != empleadoResult.Value && !currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure<Guid>(Error.Forbidden("PosSesionCaja.NoPropia", "Only the cashier who opened the session or an administrator can close it."), 403);
        }

        var montoCalculado = await ArqueoCajaCalculator.CalcularEfectivoEsperadoAsync(dbContext, sesion, cancellationToken);

        var result = sesion.Cerrar(DateTime.UtcNow, request.MontoCierre, montoCalculado);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(sesion.Id);
    }
}
