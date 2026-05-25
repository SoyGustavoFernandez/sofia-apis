using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Queries.GetUnidadMedidaById;

public record GetUnidadMedidaByIdQuery(Guid Id) : IRequest<Result<UnidadMedidaDto>>;

public class GetUnidadMedidaByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUnidadMedidaByIdQuery, Result<UnidadMedidaDto>>
{
    public async Task<Result<UnidadMedidaDto>> Handle(GetUnidadMedidaByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.UnidadesMedida
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<UnidadMedidaDto>(Error.NotFound("UnidadMedida.NotFound", $"Unidad de Medida with ID {request.Id} was not found."), 404);
        }

        var dto = new UnidadMedidaDto(
            entity.Id,
            entity.Codigo,
            entity.Descripcion);

        return Result.Success(dto);
    }
}
