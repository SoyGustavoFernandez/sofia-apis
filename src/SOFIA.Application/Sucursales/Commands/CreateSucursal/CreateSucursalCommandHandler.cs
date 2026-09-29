using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Sucursales.Commands.CreateSucursal;

public class CreateSucursalCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<CreateSucursalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSucursalCommand request, CancellationToken cancellationToken)
    {
        var empresaResult = currentUser.GetEmpresaId();
        if (empresaResult.IsFailure)
        {
            return Result.Failure<Guid>(empresaResult.Error);
        }

        var result = Sucursal.Create(
            request.Nombre,
            request.DireccionFisica,
            request.NumeroLicencia,
            request.GerenteId,
            empresaResult.Value);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var duplicado = await context.Sucursales
            .AnyAsync(s => s.Numero_Licencia == request.NumeroLicencia && !s.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure<Guid>(Error.Conflict("Sucursal.NumeroLicencia.Duplicado", "Another branch already uses this license number."), 409);
        }

        // Tenant-filtered lookup: an employee of another company must not be stored as manager
        if (request.GerenteId is { } gerenteId && !await context.Empleados.AnyAsync(e => e.Id == gerenteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Empleado.NotFound", "El empleado especificado no existe."), 404);
        }

        _ = context.Sucursales.Add(result.Value);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
