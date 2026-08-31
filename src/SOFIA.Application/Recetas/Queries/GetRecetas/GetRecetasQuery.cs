using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.GetRecetas;

public record RecetaResumenDto(Guid Id, Guid ClienteId, Guid MedicoId, DateOnly FechaExpedicion, string? IndicacionesUso);

public record GetRecetasQuery(Guid? ClienteId, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<RecetaResumenDto>>>;
