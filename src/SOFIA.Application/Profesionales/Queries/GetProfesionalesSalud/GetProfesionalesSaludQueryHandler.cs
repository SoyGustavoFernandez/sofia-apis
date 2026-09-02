using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Queries.GetProfesionalesSalud;

public class GetProfesionalesSaludQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProfesionalesSaludQuery, Result<PaginatedList<ProfesionalSaludDto>>>
{
    public async Task<Result<PaginatedList<ProfesionalSaludDto>>> Handle(GetProfesionalesSaludQuery request, CancellationToken cancellationToken)
    {
        var query = context.ProfesionalesSalud
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.NumeroRegistro))
        {
            query = query.Where(x => x.NumeroRegistro.Contains(request.NumeroRegistro));
        }

        if (!string.IsNullOrWhiteSpace(request.NombrePrescriptor))
        {
            query = query.Where(x => x.NombrePrescriptor.Contains(request.NombrePrescriptor));
        }

        var paginatedList = await PaginatedList<ProfesionalSaludDto>.CreateAsync(
            query.Select(x => new ProfesionalSaludDto(x.Id, x.NumeroRegistro, x.NombrePrescriptor, x.DireccionClinica)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
