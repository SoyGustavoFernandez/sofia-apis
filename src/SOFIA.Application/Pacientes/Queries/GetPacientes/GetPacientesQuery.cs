using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Queries.GetPacientes;

public record PacienteDto(Guid Id, string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario);

public record GetPacientesQuery : IRequest<Result<PaginatedList<PacienteDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetPacientesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPacientesQuery, Result<PaginatedList<PacienteDto>>>
{
    public async Task<Result<PaginatedList<PacienteDto>>> Handle(GetPacientesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Pacientes
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.DocIdentidadGub.ToLower().Contains(searchTerm) ||
                                     x.NombreApellidos.ToLower().Contains(searchTerm));
        }

        var paginatedList = await PaginatedList<PacienteDto>.CreateAsync(
            query.Select(x => new PacienteDto(x.Id, x.DocIdentidadGub, x.NombreApellidos, x.FechaNacimiento, x.ContactoPrimario)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
