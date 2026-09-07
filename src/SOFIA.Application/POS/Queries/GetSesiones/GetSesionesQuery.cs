using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Queries.GetSesiones;

public record SesionResumenDto(
    Guid Id,
    Guid SucursalId,
    string SucursalNombre,
    Guid EmpleadoId,
    string EmpleadoNombre,
    DateTime FechaHoraApertura,
    DateTime? FechaHoraCierre,
    decimal MontoAperturaEfectivo,
    decimal? MontoCierreCalculado,
    decimal? MontoCierreDeclarado,
    decimal? DiferenciaArqueo,
    Domain.Enums.EstadoSesion EstadoSesion);

public record GetSesionesQuery(Guid? SucursalId, Domain.Enums.EstadoSesion? EstadoSesion, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<SesionResumenDto>>>;
