using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Magistrales.Queries.GetOrdenes;

public record OrdenResumenDto(Guid Id, Guid SucursalId, Guid ProductoResultanteId, decimal? CantidadProducida, string EstadoProduccion, DateTime FechaPreparacion);

public record GetOrdenesQuery(Guid? SucursalId, string? EstadoProduccion, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<OrdenResumenDto>>>;
