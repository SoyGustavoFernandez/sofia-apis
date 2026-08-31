using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Queries.GetProfesionalSaludById;

public record ProfesionalSaludDto(Guid Id, string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica);

public record GetProfesionalSaludByIdQuery(Guid Id) : IRequest<Result<ProfesionalSaludDto>>;

public class GetProfesionalSaludByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProfesionalSaludByIdQuery, Result<ProfesionalSaludDto>>
{
    public async Task<Result<ProfesionalSaludDto>> Handle(GetProfesionalSaludByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.ProfesionalesSalud
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<ProfesionalSaludDto>(Error.NotFound("ProfesionalSalud.NotFound", $"ProfesionalSalud with ID {request.Id} was not found."), 404);
        }

        var dto = new ProfesionalSaludDto(
            entity.Id,
            entity.NumeroRegistro,
            entity.NombrePrescriptor,
            entity.DireccionClinica);

        return Result.Success(dto);
    }
}
