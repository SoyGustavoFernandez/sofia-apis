using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Profesionales.Queries.GetProfesionalesSalud;

public record ProfesionalSaludDto(Guid Id, string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica);

public record GetProfesionalesSaludQuery : IRequest<Result<PaginatedList<ProfesionalSaludDto>>>
{
    public string? NumeroRegistro { get; init; }
    public string? NombrePrescriptor { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
