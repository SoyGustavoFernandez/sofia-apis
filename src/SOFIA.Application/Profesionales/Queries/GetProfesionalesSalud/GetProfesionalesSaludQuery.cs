using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Queries.GetProfesionalesSalud;

public record ProfesionalSaludDto(Guid Id, string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica);

public record GetProfesionalesSaludQuery : IRequest<Result<PaginatedList<ProfesionalSaludDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

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
