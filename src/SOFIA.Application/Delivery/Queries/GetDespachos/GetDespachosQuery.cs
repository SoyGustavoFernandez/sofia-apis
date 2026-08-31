using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Queries.GetDespachos;

public record DespachoResumenDto(Guid Id, Guid VentaId, Domain.Enums.EstadoDespacho EstadoDespacho, string DireccionEntrega);

public record GetDespachosQuery(Domain.Enums.EstadoDespacho? EstadoDespacho, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<DespachoResumenDto>>>;
