using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiaUoMById;

public record GetJerarquiaUoMByIdQuery(Guid Id) : IRequest<Result<JerarquiaUoMDto>>;

public class GetJerarquiaUoMByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetJerarquiaUoMByIdQuery, Result<JerarquiaUoMDto>>
{
    public async Task<Result<JerarquiaUoMDto>> Handle(GetJerarquiaUoMByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.JerarquiasUoM
            .AsNoTracking()
            .Include(x => x.Producto)
            .Include(x => x.UnidadMayor)
            .Include(x => x.UnidadMenor)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<JerarquiaUoMDto>(Error.NotFound("JerarquiaUoM.NotFound", $"Jerarquía with ID {request.Id} was not found."), 404);
        }

        var dto = new JerarquiaUoMDto(
            entity.Id,
            entity.ProductoId,
            entity.Producto?.NombreComercial ?? "Unknown",
            entity.UnidadMayorId,
            entity.UnidadMayor?.Descripcion ?? "Unknown",
            entity.UnidadMenorId,
            entity.UnidadMenor?.Descripcion ?? "Unknown",
            entity.Multiplicador);

        return Result.Success(dto);
    }
}
