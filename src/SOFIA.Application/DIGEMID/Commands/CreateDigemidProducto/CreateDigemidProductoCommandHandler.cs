using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.DIGEMID.Commands.CreateDigemidProducto;

public class CreateDigemidProductoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateDigemidProductoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateDigemidProductoCommand request, CancellationToken cancellationToken)
    {
        var result = DigemidCatalogoProducto.Create(
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
            return Result.Failure<Guid>(result.Error);
        }

        var duplicado = await context.DigemidCatalogoProductos
            .AnyAsync(p => p.CodProd == request.CodProd && !p.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure<Guid>(Error.Conflict("DigemidCatalogo.CodProd.Duplicado", "Another catalog product already uses this code."), 409);
        }

        _ = context.DigemidCatalogoProductos.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
