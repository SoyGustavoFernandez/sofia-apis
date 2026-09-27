using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.POS.Commands.AperturarCaja;

public class AperturarCajaCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<AperturarCajaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AperturarCajaCommand request, CancellationToken cancellationToken)
    {
        // Cashier, branch and opening time come from the session so the cash count is always attributed to its real owner
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<Guid>(sucursalResult.Error);
        }

        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<Guid>(empleadoResult.Error);
        }

        var yaTieneCajaAbierta = await dbContext.POSSesionesCaja
            .AnyAsync(s => s.EmpleadoId == empleadoResult.Value && s.EstadoSesion == EstadoSesion.Abierta, cancellationToken);
        if (yaTieneCajaAbierta)
        {
            return Result.Failure<Guid>(Error.Conflict("PosSesionCaja.YaAbierta", "Ya tienes una caja abierta. Ciérrala antes de abrir otra."), 409);
        }

        var createResult = Domain.Entities.PosSesionCaja.Create(sucursalResult.Value, empleadoResult.Value, DateTime.UtcNow, request.MontoAperturaEfectivo);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.POSSesionesCaja.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);
    }
}
