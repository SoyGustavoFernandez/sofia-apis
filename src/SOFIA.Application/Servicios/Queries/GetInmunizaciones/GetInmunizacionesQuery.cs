using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Queries.GetInmunizaciones;

public record InmunizacionResumenDto(Guid Id, Guid ClienteId, DateTime FechaAdmnFisica, string ViaAdministracion);

public record GetInmunizacionesQuery(Guid? ClienteId, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<InmunizacionResumenDto>>>;
