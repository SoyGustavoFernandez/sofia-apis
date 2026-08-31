using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogoById;

public class GetDigemidCatalogoByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetDigemidCatalogoByIdQuery, Result<DigemidProductoDto>>
{
    public async Task<Result<DigemidProductoDto>> Handle(GetDigemidCatalogoByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.DigemidCatalogoProductos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<DigemidProductoDto>(Error.NotFound("DigemidCatalogo.NotFound", $"DigemidCatalogoProducto with ID {request.Id} was not found."), 404);
        }

        var dto = new DigemidProductoDto(
            entity.Id,
            entity.CodProd,
            entity.NomProd,
            entity.Concent,
            entity.FormaFarmaceutica,
            entity.Fraccion,
            entity.RegistroSanitario,
            entity.Titular,
            entity.Estado);

        return Result.Success(dto);
    }
}
