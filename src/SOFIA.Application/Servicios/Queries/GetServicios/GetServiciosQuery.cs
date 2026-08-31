using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Queries.GetServicios;

public record ServicioResumenDto(Guid Id, Guid ClienteId, Guid ProductoId, DateTime FechaHoraProgramada, string EstadoCita);

public record GetServiciosQuery(string? EstadoCita, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<ServicioResumenDto>>>;
