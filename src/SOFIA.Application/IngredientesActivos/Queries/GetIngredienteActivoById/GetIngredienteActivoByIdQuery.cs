using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Queries.GetIngredienteActivoById;

public record GetIngredienteActivoByIdQuery(Guid Id) : IRequest<Result<IngredienteActivoDto>>;

public class GetIngredienteActivoByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetIngredienteActivoByIdQuery, Result<IngredienteActivoDto>>
{
    public async Task<Result<IngredienteActivoDto>> Handle(GetIngredienteActivoByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.IngredientesActivos
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<IngredienteActivoDto>(Error.NotFound("IngredienteActivo.NotFound", $"Ingrediente Activo with ID {request.Id} was not found."), 404);
        }

        var dto = new IngredienteActivoDto(
            entity.Id,
            entity.DenominacionDci,
            entity.CodigoAtc);

        return Result.Success(dto);
    }
}
