using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Queries.GetAseguradoraById;

public class GetAseguradoraByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAseguradoraByIdQuery, Result<AseguradoraDto>>
{
    public async Task<Result<AseguradoraDto>> Handle(GetAseguradoraByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Aseguradoras
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<AseguradoraDto>(Error.NotFound("Aseguradora.NotFound", $"Aseguradora with ID {request.Id} was not found."), 404);
        }

        var dto = new AseguradoraDto(
            entity.Id,
            entity.NombreComercial,
            entity.CodigoIdentificadorNacional);

        return Result.Success(dto);
    }
}
