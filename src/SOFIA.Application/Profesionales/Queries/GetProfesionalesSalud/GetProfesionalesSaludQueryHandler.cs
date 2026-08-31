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

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.NumeroRegistro.ToLower().Contains(searchTerm) ||
                                     x.NombrePrescriptor.ToLower().Contains(searchTerm));
        }

        var paginatedList = await PaginatedList<ProfesionalSaludDto>.CreateAsync(
            query.Select(x => new ProfesionalSaludDto(x.Id, x.NumeroRegistro, x.NombrePrescriptor, x.DireccionClinica)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
