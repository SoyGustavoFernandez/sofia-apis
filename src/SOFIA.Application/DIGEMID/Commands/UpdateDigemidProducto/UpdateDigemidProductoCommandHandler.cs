using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.UpdateDigemidProducto;

public class UpdateDigemidProductoCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateDigemidProductoCommand, Result>
{
    public async Task<Result> Handle(UpdateDigemidProductoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.DigemidCatalogoProductos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null || entity.IsDeleted)
        {
            return Result.Failure(Error.NotFound("DigemidCatalogo.NotFound", $"DigemidCatalogoProducto with ID {request.Id} was not found."), 404);
        }

        var duplicado = await context.DigemidCatalogoProductos
            .AnyAsync(p => p.Id != entity.Id && p.CodProd == request.CodProd && !p.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure(Error.Conflict("DigemidCatalogo.CodProd.Duplicado", "Another catalog product already uses this code."), 409);
        }

        var result = entity.Update(
            request.CodProd,
            request.NomProd,
            request.Concent,
            request.FormaFarmaceutica,
            request.Fraccion,
            request.RegistroSanitario,
            request.Titular,
            request.Estado);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
