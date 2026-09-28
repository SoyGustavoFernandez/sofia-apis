using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Inventarios.Commands.RegisterInventario;

public class RegisterInventarioHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess)
    : IRequestHandler<RegisterInventarioCommand, Result<Guid>>
{
    private const string InventarioSucursalIndexName = "UX_Inventario_Sucursal_Lote";

    public async Task<Result<Guid>> Handle(RegisterInventarioCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Sucursal existence
        var sucursalExists = await context.Sucursales.AnyAsync(s => s.Id == request.SucursalId, cancellationToken);
        if (!sucursalExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Sucursal.NotFound", "The specified branch does not exist."));
        }

        if (!await sucursalAccess.CanAccessAsync(request.SucursalId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Forbidden("Inventario.Sucursal.NoPermitida", "You are not allowed to operate on this branch's stock."), 403);
        }

        // 2. Verify Lote existence
        var loteExists = await context.LotesInventario.AnyAsync(l => l.Id == request.LoteId, cancellationToken);
        if (!loteExists)
        {
            return Result.Failure<Guid>(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."));
        }

        // 3. Check if already exists in this branch
        var existingEntry = await context.LotesEnSucursal
            .FirstOrDefaultAsync(x => x.SucursalId == request.SucursalId && x.LoteId == request.LoteId, cancellationToken);

        if (existingEntry != null)
        {
            if (request.EsAjusteDirecto)
            {
                existingEntry.UpdateStock(request.Cantidad);
            }
            else
            {
                existingEntry.AddStock(request.Cantidad);
            }

            _ = await context.SaveChangesAsync(cancellationToken);
            return Result.Success(existingEntry.Id);
        }

        // 4. Create new entry
        var result = InventarioSucursal.Create(request.SucursalId, request.LoteId, request.Cantidad);
        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.LotesEnSucursal.Add(result.Value);

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.IsUniqueIndexViolation(InventarioSucursalIndexName))
        {
            // A concurrent request created this branch/batch row first; the caller can retry and it will add to it
            return Result.Failure<Guid>(Error.Conflict("InventarioSucursal.RegistroConcurrente", "The stock row for this branch and batch was created concurrently; retry."), 409);
        }

        return Result.Success(result.Value.Id, 201);
    }
}
