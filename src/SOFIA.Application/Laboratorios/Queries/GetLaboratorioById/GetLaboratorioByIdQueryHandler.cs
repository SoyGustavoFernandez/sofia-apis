using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Queries.GetLaboratorioById;

public class GetLaboratorioByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetLaboratorioByIdQuery, Result<LaboratorioDto>>
{
    public async Task<Result<LaboratorioDto>> Handle(GetLaboratorioByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.Laboratorios
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<LaboratorioDto>(Error.NotFound("Laboratorio.NotFound", $"Laboratorio with ID {request.Id} was not found."), 404);
        }

        var dto = new LaboratorioDto(
            entity.Id,
            entity.NombreCompania,
            entity.CodigoIdentificador);

        return Result.Success(dto);
    }
}
